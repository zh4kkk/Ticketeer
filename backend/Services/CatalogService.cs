using Microsoft.EntityFrameworkCore;
using Ticketeer.Data;
using Ticketeer.DTOs;
using Ticketeer.Models;

namespace Ticketeer.Services;

public sealed class CatalogService(AppDbContext db)
{
    public async Task<IReadOnlyList<CatalogTopicResponse>> GetTopicsAsync(string? search, CancellationToken cancellationToken)
    {
        var pattern = CatalogSearch.Pattern(search);
        var query = db.Topics.AsNoTracking();
        if (pattern is not null)
        {
            query = query.Where(x => EF.Functions.ILike(x.Name, pattern, "\\"));
        }

        return await query.OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Select(x => new CatalogTopicResponse(x.Id, x.Name, x.Description, x.Questions.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResponse<CatalogTicketListResponse>> GetTicketsAsync(
        int userId,
        string? search,
        int? topicId,
        bool? isCompleted,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidatePaging(page, pageSize);
        var pattern = CatalogSearch.Pattern(search);
        var query = db.Tickets.AsNoTracking();
        if (pattern is not null)
        {
            query = query.Where(x => EF.Functions.ILike(x.Title, pattern, "\\") ||
                                     (x.Description != null && EF.Functions.ILike(x.Description, pattern, "\\")));
        }
        if (topicId is not null)
        {
            query = query.Where(x => x.TicketQuestions.Any(question => question.Question.TopicId == topicId.Value));
        }

        var completed = db.Attempts.AsNoTracking().Where(attempt =>
            attempt.UserId == userId && attempt.Kind == AttemptKind.Training &&
            attempt.Source == AttemptSource.Prepared && attempt.Status == AttemptStatus.Completed &&
            attempt.TicketId != null);
        if (isCompleted == true)
        {
            query = query.Where(ticket => completed.Any(attempt => attempt.TicketId == ticket.Id));
        }
        else if (isCompleted == false)
        {
            query = query.Where(ticket => !completed.Any(attempt => attempt.TicketId == ticket.Id));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Title).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(ticket => new CatalogTicketListResponse(
                ticket.Id,
                ticket.Title,
                ticket.Description,
                ticket.TicketQuestions.Count,
                completed.Any(attempt => attempt.TicketId == ticket.Id)))
            .ToListAsync(cancellationToken);
        return new PagedResponse<CatalogTicketListResponse>(items, page, pageSize, totalCount);
    }

    public async Task<CatalogTicketResponse?> GetTicketAsync(int id, int userId, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets.AsNoTracking()
            .Include(x => x.TicketQuestions).ThenInclude(x => x.Question).ThenInclude(x => x.Topic)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (ticket is null)
        {
            return null;
        }

        var isCompleted = await db.Attempts.AsNoTracking().AnyAsync(attempt =>
            attempt.UserId == userId && attempt.Kind == AttemptKind.Training &&
            attempt.Source == AttemptSource.Prepared && attempt.Status == AttemptStatus.Completed &&
            attempt.TicketId == id, cancellationToken);
        var topics = ticket.TicketQuestions.Select(x => x.Question.Topic)
            .DistinctBy(x => x.Id).OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Select(x => new CatalogTopicReference(x.Id, x.Name)).ToArray();
        return new CatalogTicketResponse(ticket.Id, ticket.Title, ticket.Description,
            ticket.TicketQuestions.Count, isCompleted, topics);
    }

    public async Task<PagedResponse<CatalogExamListResponse>> GetExamsAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        ValidatePaging(page, pageSize);
        var pattern = CatalogSearch.Pattern(search);
        var query = db.Exams.AsNoTracking();
        if (pattern is not null)
        {
            query = query.Where(x => EF.Functions.ILike(x.Title, pattern, "\\") ||
                                     (x.Description != null && EF.Functions.ILike(x.Description, pattern, "\\")));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Title).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new CatalogExamListResponse(x.Id, x.Title, x.Description, x.ExamQuestions.Count,
                AttemptService.ExamTimeLimitSeconds, AttemptService.ExamMaxWrongAnswers))
            .ToListAsync(cancellationToken);
        return new PagedResponse<CatalogExamListResponse>(items, page, pageSize, totalCount);
    }

    public async Task<CatalogExamResponse?> GetExamAsync(int id, CancellationToken cancellationToken) =>
        await db.Exams.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new CatalogExamResponse(x.Id, x.Title, x.Description, x.ExamQuestions.Count,
                AttemptService.ExamTimeLimitSeconds, AttemptService.ExamMaxWrongAnswers))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<PagedResponse<CatalogQuestionListResponse>> GetQuestionsAsync(
        string? search, int? topicId, int page, int pageSize, CancellationToken cancellationToken)
    {
        ValidatePaging(page, pageSize);
        var pattern = CatalogSearch.Pattern(search);
        var query = db.Questions.AsNoTracking();
        if (pattern is not null)
        {
            query = query.Where(x => EF.Functions.ILike(x.Text, pattern, "\\"));
        }
        if (topicId is not null)
        {
            query = query.Where(x => x.TopicId == topicId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new CatalogQuestionListResponse(x.Id, x.TopicId, x.Topic.Name, x.Text))
            .ToListAsync(cancellationToken);
        return new PagedResponse<CatalogQuestionListResponse>(items, page, pageSize, totalCount);
    }

    public async Task<CatalogQuestionResponse?> GetQuestionAsync(int id, CancellationToken cancellationToken)
    {
        var question = await db.Questions.AsNoTracking().Include(x => x.Topic).Include(x => x.AnswerOptions)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return question is null ? null : new CatalogQuestionResponse(
            question.Id, question.TopicId, question.Topic.Name, question.Text,
            question.AnswerOptions.OrderBy(x => x.Position)
                .Select(x => new CatalogAnswerOptionResponse(x.Position, x.Text)).ToArray());
    }

    private static void ValidatePaging(int page, int pageSize)
    {
        if (page <= 0 || pageSize <= 0 || pageSize > 100 || (long)(page - 1) * pageSize > int.MaxValue)
        {
            throw new ValidationException("Page must be positive and pageSize must be between 1 and 100.");
        }
    }
}

internal static class CatalogSearch
{
    public static string? Pattern(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        var value = search.Trim();
        if (value.Length > 200)
        {
            throw new ValidationException("Search text cannot exceed 200 characters.");
        }

        var escaped = value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
        return $"%{escaped}%";
    }

}
