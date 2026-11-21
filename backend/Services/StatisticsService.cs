using System.Data;
using Microsoft.EntityFrameworkCore;
using Ticketeer.Data;
using Ticketeer.DTOs;
using Ticketeer.Models;

namespace Ticketeer.Services;

public sealed class StatisticsService(AppDbContext db, AttemptService attemptService)
{
    private static readonly AttemptStatus[] FinishedStatuses =
    {
        AttemptStatus.Completed,
        AttemptStatus.Failed,
        AttemptStatus.TimedOut
    };

    public async Task<PagedResponse<AttemptHistoryItem>> GetAttemptHistoryAsync(
        int userId,
        int page,
        int pageSize,
        AttemptKind? kind,
        AttemptSource? source,
        AttemptStatus? status,
        CancellationToken cancellationToken)
    {
        ValidatePaging(page, pageSize);
        ValidateFilters(kind, source, status);
        await attemptService.ExpireExamTimeoutsAsync(userId, null, cancellationToken);

        var query = db.Attempts.AsNoTracking().Where(x => x.UserId == userId);
        query = ApplyFilters(query, kind, source, status);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.StartedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToHistoryItem())
            .ToListAsync(cancellationToken);

        return new PagedResponse<AttemptHistoryItem>(items, page, pageSize, totalCount);
    }

    public async Task<StatisticsResponse> GetMyStatisticsAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        await attemptService.ExpireExamTimeoutsAsync(userId, null, cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var result = await BuildStatisticsAsync(userId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<ActivityResponse>> GetActivityAsync(
        int userId,
        int days,
        CancellationToken cancellationToken)
    {
        ValidateDays(days);
        await attemptService.ExpireExamTimeoutsAsync(userId, null, cancellationToken);
        return await BuildActivityAsync(userId, days, cancellationToken);
    }

    public async Task<PagedResponse<UserStatisticsResponse>> GetUserStatisticsListAsync(
        int page,
        int pageSize,
        int? groupId,
        CancellationToken cancellationToken)
    {
        ValidatePaging(page, pageSize);
        if (groupId is <= 0)
        {
            throw new ValidationException("GroupId must be positive.");
        }
        var usersQuery = db.Users.AsNoTracking()
            .Where(x => x.Role == UserRole.Cashier);
        if (groupId is not null)
        {
            usersQuery = usersQuery.Where(x => x.GroupId == groupId.Value);
        }

        var totalCount = await usersQuery.CountAsync(cancellationToken);
        var users = await usersQuery
            .Include(x => x.Group)
            .OrderBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(x => x.Id).ToArray();
        if (userIds.Length == 0)
        {
            return new PagedResponse<UserStatisticsResponse>(Array.Empty<UserStatisticsResponse>(), page, pageSize, totalCount);
        }
        await attemptService.ExpireExamTimeoutsForUsersAsync(userIds, cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var aggregates = await GetAggregatesAsync(
            db.Attempts.AsNoTracking().Where(x => userIds.Contains(x.UserId)),
            cancellationToken);
        var totalPreparedTickets = await db.Tickets.CountAsync(cancellationToken);

        // The list exposes per-user progress; calculate it with each user's distinct tickets.
        var completedTicketsByUser = await db.Attempts.AsNoTracking()
            .Where(x => userIds.Contains(x.UserId) && x.Kind == AttemptKind.Training &&
                        x.Source == AttemptSource.Prepared && x.Status == AttemptStatus.Completed &&
                        x.TicketId != null)
            .GroupBy(x => x.UserId)
            .Select(group => new { UserId = group.Key, Count = group.Select(x => x.TicketId).Distinct().Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);
        var items = users.Select(user => new UserStatisticsResponse(
            user.Id,
            user.Login,
            user.FullName,
            user.GroupId,
            user.Group?.Name,
            user.IsActive,
            ToStatistics(aggregates.GetValueOrDefault(user.Id), totalPreparedTickets,
                completedTicketsByUser.GetValueOrDefault(user.Id)))).ToArray();
        await transaction.CommitAsync(cancellationToken);
        return new PagedResponse<UserStatisticsResponse>(items, page, pageSize, totalCount);
    }

    public async Task<UserStatisticsResponse?> GetUserStatisticsAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().Include(x => x.Group)
            .SingleOrDefaultAsync(x => x.Id == userId && x.Role == UserRole.Cashier, cancellationToken);
        if (user is null)
        {
            return null;
        }

        await attemptService.ExpireExamTimeoutsAsync(userId, null, cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var totalPreparedTickets = await db.Tickets.CountAsync(cancellationToken);
        var completedPreparedTickets = await db.Attempts.AsNoTracking()
            .Where(x => x.UserId == userId && x.Kind == AttemptKind.Training &&
                        x.Source == AttemptSource.Prepared && x.Status == AttemptStatus.Completed &&
                        x.TicketId != null)
            .Select(x => x.TicketId!.Value)
            .Distinct()
            .CountAsync(cancellationToken);
        var statistics = await BuildStatisticsAsync(userId, cancellationToken, totalPreparedTickets, completedPreparedTickets);
        await transaction.CommitAsync(cancellationToken);
        return new UserStatisticsResponse(user.Id, user.Login, user.FullName, user.GroupId, user.Group?.Name, user.IsActive, statistics);
    }

    public async Task<IReadOnlyList<ActivityResponse>> GetUserActivityAsync(
        int userId,
        int days,
        CancellationToken cancellationToken)
    {
        ValidateDays(days);
        var exists = await db.Users.AsNoTracking().AnyAsync(x => x.Id == userId && x.Role == UserRole.Cashier, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException("Cashier was not found.");
        }

        await attemptService.ExpireExamTimeoutsAsync(userId, null, cancellationToken);
        return await BuildActivityAsync(userId, days, cancellationToken);
    }

    public async Task<PagedResponse<AttemptHistoryItem>> GetUserAttemptHistoryAsync(
        int userId,
        int page,
        int pageSize,
        AttemptKind? kind,
        AttemptSource? source,
        AttemptStatus? status,
        CancellationToken cancellationToken)
    {
        var exists = await db.Users.AsNoTracking().AnyAsync(x => x.Id == userId && x.Role == UserRole.Cashier, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException("Cashier was not found.");
        }

        return await GetAttemptHistoryAsync(userId, page, pageSize, kind, source, status, cancellationToken);
    }

    public async Task<IReadOnlyList<GroupStatisticsResponse>> GetGroupStatisticsAsync(CancellationToken cancellationToken)
    {
        await attemptService.ExpireExamTimeoutsAsync(null, null, cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var groups = await db.Groups.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var aggregates = await GetGroupAggregatesAsync(cancellationToken);
        var members = await db.Users.AsNoTracking()
            .Where(x => x.Role == UserRole.Cashier && x.GroupId != null)
            .GroupBy(x => x.GroupId!.Value)
            .Select(group => new { GroupId = group.Key, Total = group.Count(), Active = group.Count(x => x.IsActive) })
            .ToDictionaryAsync(x => x.GroupId, x => (x.Total, x.Active), cancellationToken);
        var result = groups.Select(group =>
        {
            var aggregate = aggregates.GetValueOrDefault(group.Id);
            var counts = members.GetValueOrDefault(group.Id);
            return ToGroupStatistics(aggregate, group.Id, group.Name, counts.Total, counts.Active);
        }).ToArray();
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<GroupStatisticsResponse?> GetGroupStatisticsAsync(int groupId, CancellationToken cancellationToken)
    {
        await attemptService.ExpireExamTimeoutsAsync(null, groupId, cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var group = await db.Groups.AsNoTracking().SingleOrDefaultAsync(x => x.Id == groupId, cancellationToken);
        if (group is null)
        {
            return null;
        }

        var counts = await db.Users.AsNoTracking()
            .Where(x => x.Role == UserRole.Cashier && x.GroupId == groupId)
            .GroupBy(_ => 1)
            .Select(x => new { Total = x.Count(), Active = x.Count(user => user.IsActive) })
            .SingleOrDefaultAsync(cancellationToken);
        var aggregate = (await GetGroupAggregatesAsync(cancellationToken, groupId)).GetValueOrDefault(groupId);
        var result = ToGroupStatistics(aggregate, group.Id, group.Name, counts?.Total ?? 0, counts?.Active ?? 0);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task<StatisticsResponse> BuildStatisticsAsync(
        int userId,
        CancellationToken cancellationToken,
        int? totalPreparedTickets = null,
        int? completedPreparedTickets = null)
    {
        totalPreparedTickets ??= await db.Tickets.CountAsync(cancellationToken);
        completedPreparedTickets ??= await db.Attempts.AsNoTracking()
            .Where(x => x.UserId == userId && x.Kind == AttemptKind.Training && x.Source == AttemptSource.Prepared &&
                        x.Status == AttemptStatus.Completed && x.TicketId != null)
            .Select(x => x.TicketId!.Value).Distinct().CountAsync(cancellationToken);
        var aggregate = (await GetAggregatesAsync(db.Attempts.AsNoTracking().Where(x => x.UserId == userId), cancellationToken))
            .GetValueOrDefault(userId);
        return ToStatistics(aggregate, totalPreparedTickets.Value, completedPreparedTickets.Value);
    }

    private async Task<IReadOnlyList<ActivityResponse>> BuildActivityAsync(int userId, int days, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var firstDay = today.AddDays(-(days - 1));
        var start = new DateTimeOffset(firstDay.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var end = start.AddDays(days);
        var logins = await db.LoginEvents.AsNoTracking()
            .Where(x => x.UserId == userId && x.CreatedAt >= start && x.CreatedAt < end)
            .Select(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var attempts = await db.Attempts.AsNoTracking()
            .Where(x => x.UserId == userId && x.FinishedAt != null && x.FinishedAt >= start && x.FinishedAt < end)
            .Select(x => new ActivityAttempt(x.FinishedAt!.Value, x.Kind, x.Status))
            .ToListAsync(cancellationToken);
        var loginCounts = logins.GroupBy(ToDate).ToDictionary(x => x.Key, x => x.Count());
        var attemptCounts = attempts.GroupBy(x => ToDate(x.FinishedAt)).ToDictionary(
            x => x.Key,
            x => (Training: x.Count(item => item.Kind == AttemptKind.Training && item.Status == AttemptStatus.Completed),
                Exam: x.Count(item => item.Kind == AttemptKind.Exam && FinishedStatuses.Contains(item.Status))));

        return Enumerable.Range(0, days).Select(offset =>
        {
            var date = firstDay.AddDays(offset);
            var counts = attemptCounts.GetValueOrDefault(date);
            return new ActivityResponse(date, loginCounts.GetValueOrDefault(date), counts.Training, counts.Exam);
        }).ToArray();
    }

    private async Task<Dictionary<int, AggregateRow>> GetAggregatesAsync(
        IQueryable<Attempt> query,
        CancellationToken cancellationToken) =>
        await query
            .GroupBy(x => x.UserId)
            .Select(group => new AggregateRow(
                group.Key,
                group.Count(),
                group.Count(x => x.Status != AttemptStatus.InProgress && x.Kind == AttemptKind.Training && x.Status == AttemptStatus.Completed),
                group.Count(x => x.Status != AttemptStatus.InProgress && x.Kind == AttemptKind.Exam),
                group.Count(x => x.Status != AttemptStatus.InProgress && x.Kind == AttemptKind.Exam && x.Status == AttemptStatus.Completed && x.IsPassed == true),
                group.Count(x => x.Status != AttemptStatus.InProgress && x.Kind == AttemptKind.Exam && x.IsPassed == false),
                group.Sum(x => x.Status != AttemptStatus.InProgress ? x.CorrectAnswers : 0),
                group.Sum(x => x.Status != AttemptStatus.InProgress ? x.WrongAnswers : 0)))
            .ToDictionaryAsync(x => x.UserId, cancellationToken);

    private async Task<Dictionary<int, AggregateRow>> GetGroupAggregatesAsync(
        CancellationToken cancellationToken,
        int? groupId = null)
    {
        var query = db.Attempts.AsNoTracking()
            .Where(x => x.GroupIdSnapshot != null);
        if (groupId is not null)
        {
            query = query.Where(x => x.GroupIdSnapshot == groupId.Value);
        }

        return await query
            .GroupBy(x => x.GroupIdSnapshot!.Value)
            .Select(group => new AggregateRow(
                group.Key,
                group.Count(),
                group.Count(x => x.Status != AttemptStatus.InProgress && x.Kind == AttemptKind.Training && x.Status == AttemptStatus.Completed),
                group.Count(x => x.Status != AttemptStatus.InProgress && x.Kind == AttemptKind.Exam),
                group.Count(x => x.Status != AttemptStatus.InProgress && x.Kind == AttemptKind.Exam && x.Status == AttemptStatus.Completed && x.IsPassed == true),
                group.Count(x => x.Status != AttemptStatus.InProgress && x.Kind == AttemptKind.Exam && x.IsPassed == false),
                group.Sum(x => x.Status != AttemptStatus.InProgress ? x.CorrectAnswers : 0),
                group.Sum(x => x.Status != AttemptStatus.InProgress ? x.WrongAnswers : 0)))
            .ToDictionaryAsync(x => x.UserId, cancellationToken);
    }

    private static StatisticsResponse ToStatistics(AggregateRow? aggregate, int totalTickets, int completedTickets)
    {
        var value = aggregate ?? new AggregateRow(0, 0, 0, 0, 0, 0, 0, 0);
        return new StatisticsResponse(
            value.TotalAttempts,
            value.CompletedTrainingCount,
            value.FinishedExamCount,
            value.PassedExamCount,
            value.FailedExamCount,
            value.CorrectAnswers,
            value.WrongAnswers,
            Percentage(value.CorrectAnswers, value.CorrectAnswers + value.WrongAnswers),
            Percentage(value.PassedExamCount, value.FinishedExamCount),
            totalTickets,
            completedTickets,
            Percentage(completedTickets, totalTickets));
    }

    private static GroupStatisticsResponse ToGroupStatistics(
        AggregateRow? aggregate,
        int groupId,
        string groupName,
        int employeeCount,
        int activeEmployeeCount)
    {
        var value = aggregate ?? new AggregateRow(0, 0, 0, 0, 0, 0, 0, 0);
        return new GroupStatisticsResponse(
            groupId,
            groupName,
            employeeCount,
            activeEmployeeCount,
            value.CompletedTrainingCount,
            value.FinishedExamCount,
            value.PassedExamCount,
            value.FailedExamCount,
            value.CorrectAnswers,
            value.WrongAnswers,
            Percentage(value.CorrectAnswers, value.CorrectAnswers + value.WrongAnswers),
            Percentage(value.PassedExamCount, value.FinishedExamCount));
    }

    private static IQueryable<Attempt> ApplyFilters(IQueryable<Attempt> query, AttemptKind? kind, AttemptSource? source, AttemptStatus? status)
    {
        if (kind is not null) query = query.Where(x => x.Kind == kind.Value);
        if (source is not null) query = query.Where(x => x.Source == source.Value);
        if (status is not null) query = query.Where(x => x.Status == status.Value);
        return query;
    }

    private static System.Linq.Expressions.Expression<Func<Attempt, AttemptHistoryItem>> ToHistoryItem() =>
        x => new AttemptHistoryItem(x.Id, x.Kind, x.Source, x.Status, x.TicketId, x.Ticket == null ? null : x.Ticket.Title,
            x.ExamId, x.Exam == null ? null : x.Exam.Title, x.StartedAt, x.FinishedAt, x.QuestionCount,
            x.CorrectAnswers, x.WrongAnswers, x.IsPassed);

    private static DateOnly ToDate(DateTimeOffset timestamp) => DateOnly.FromDateTime(timestamp.UtcDateTime);

    private static decimal Percentage(int numerator, int denominator) =>
        denominator == 0 ? 0 : Math.Round((decimal)numerator * 100m / denominator, 2, MidpointRounding.AwayFromZero);

    private static void ValidatePaging(int page, int pageSize)
    {
        if (page <= 0 || pageSize <= 0 || pageSize > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            throw new ValidationException("Page must be positive and pageSize must be between 1 and 100.");
    }

    private static void ValidateDays(int days)
    {
        if (days is < 1 or > 365)
            throw new ValidationException("Days must be between 1 and 365.");
    }

    private static void ValidateFilters(AttemptKind? kind, AttemptSource? source, AttemptStatus? status)
    {
        if (kind is not null && !Enum.IsDefined(kind.Value) ||
            source is not null && !Enum.IsDefined(source.Value) ||
            status is not null && !Enum.IsDefined(status.Value))
        {
            throw new ValidationException("Attempt filter has an invalid value.");
        }
    }

    private sealed record AggregateRow(int UserId, int TotalAttempts, int CompletedTrainingCount, int FinishedExamCount,
        int PassedExamCount, int FailedExamCount, int CorrectAnswers, int WrongAnswers);

    private sealed record ActivityAttempt(DateTimeOffset FinishedAt, AttemptKind Kind, AttemptStatus Status);
}
