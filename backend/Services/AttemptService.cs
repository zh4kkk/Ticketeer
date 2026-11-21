using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ticketeer.Data;
using Ticketeer.DTOs;
using Ticketeer.Models;

namespace Ticketeer.Services;

public sealed class AttemptService(AppDbContext db)
{
    public const int ExamQuestionCount = 10;
    public const int ExamTimeLimitSeconds = 20 * 60;
    public const int ExamMaxWrongAnswers = 2;

    public async Task<int> ExpireExamTimeoutsAsync(
        int? userId,
        int? groupId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (userId is not null)
            {
                return await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE attempts
                SET status = 'TimedOut', is_passed = FALSE,
                    finished_at = started_at + make_interval(secs => time_limit_seconds)
                WHERE kind = 'Exam' AND status = 'InProgress'
                  AND time_limit_seconds IS NOT NULL
                  AND started_at + make_interval(secs => time_limit_seconds) <= CURRENT_TIMESTAMP
                  AND user_id = {userId.Value}
                """, cancellationToken);
            }

            if (groupId is not null)
            {
                return await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE attempts
                SET status = 'TimedOut', is_passed = FALSE,
                    finished_at = started_at + make_interval(secs => time_limit_seconds)
                WHERE kind = 'Exam' AND status = 'InProgress'
                  AND time_limit_seconds IS NOT NULL
                  AND started_at + make_interval(secs => time_limit_seconds) <= CURRENT_TIMESTAMP
                  AND group_id_snapshot = {groupId.Value}
                """, cancellationToken);
            }

            return await db.Database.ExecuteSqlRawAsync("""
            UPDATE attempts
            SET status = 'TimedOut', is_passed = FALSE,
                finished_at = started_at + make_interval(secs => time_limit_seconds)
            WHERE kind = 'Exam' AND status = 'InProgress'
              AND time_limit_seconds IS NOT NULL
              AND started_at + make_interval(secs => time_limit_seconds) <= CURRENT_TIMESTAMP
            """, cancellationToken);
        }
        catch (PostgresException exception) when (exception.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected)
        {
            throw new ConflictException("Attempts were changed concurrently. Try again.");
        }
    }

    public async Task<int> ExpireExamTimeoutsForUsersAsync(
        IReadOnlyCollection<int> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return 0;
        }

        try
        {
            var ids = userIds.Distinct().ToArray();
            return await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE attempts
                SET status = 'TimedOut', is_passed = FALSE,
                    finished_at = started_at + make_interval(secs => time_limit_seconds)
                WHERE kind = 'Exam' AND status = 'InProgress'
                  AND time_limit_seconds IS NOT NULL
                  AND started_at + make_interval(secs => time_limit_seconds) <= CURRENT_TIMESTAMP
                  AND user_id = ANY({ids})
                """, cancellationToken);
        }
        catch (PostgresException exception) when (exception.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected)
        {
            throw new ConflictException("Attempts were changed concurrently. Try again.");
        }
    }

    public Task<AttemptResponse> StartTrainingTicketAsync(
        int ticketId,
        int userId,
        CancellationToken cancellationToken) =>
        StartAttemptAsync(
            userId,
            ticketId,
            AttemptKind.Training,
            AttemptSource.Prepared,
            (preparedSetId, token) => GetTicketQuestionsAsync(preparedSetId, token),
            cancellationToken);

    public Task<AttemptResponse> StartExamAsync(
        int examId,
        int userId,
        CancellationToken cancellationToken) =>
        StartAttemptAsync(
            userId,
            examId,
            AttemptKind.Exam,
            AttemptSource.Prepared,
            (preparedSetId, token) => GetExamQuestionsAsync(preparedSetId, token),
            cancellationToken);

    public Task<AttemptResponse> StartRandomTrainingAsync(
        int topicId,
        int questionCount,
        int userId,
        CancellationToken cancellationToken)
    {
        if (topicId <= 0)
        {
            throw new ValidationException("TopicId must be positive.");
        }

        if (questionCount <= 0)
        {
            throw new ValidationException("QuestionCount must be positive.");
        }

        return StartAttemptAsync(
            userId,
            topicId,
            AttemptKind.Training,
            AttemptSource.Random,
            (topicIdValue, token) => GetRandomTrainingQuestionsAsync(topicIdValue, questionCount, token),
            cancellationToken);
    }

    public Task<AttemptResponse> StartRandomExamAsync(
        int userId,
        CancellationToken cancellationToken) =>
        StartAttemptAsync(
            userId,
            0,
            AttemptKind.Exam,
            AttemptSource.Random,
            (_, token) => GetRandomExamQuestionsAsync(token),
            cancellationToken);

    public async Task<AttemptResponse?> GetAsync(
        int id,
        int userId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await GetCoreAsync(id, userId, cancellationToken);
        }
        catch (PostgresException exception) when (IsAttemptConcurrencyConflict(exception))
        {
            throw new ConflictException("The attempt was changed concurrently. Try again.");
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The attempt was changed concurrently. Try again.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgresException &&
                                                  IsAttemptConcurrencyConflict(postgresException))
        {
            throw new ConflictException("The attempt was changed concurrently. Try again.");
        }
    }

    public Task<AttemptResponse?> GetForUserAsync(
        int id,
        int userId,
        CancellationToken cancellationToken) =>
        GetAsync(id, userId, cancellationToken);

    private async Task<AttemptResponse?> GetCoreAsync(
        int id,
        int userId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var attempt = await GetForUpdateAsync(id, userId, cancellationToken);
        if (attempt is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        if (IsExpired(attempt, DateTimeOffset.UtcNow))
        {
            MarkTimedOut(attempt);
            await db.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return ToResponse(attempt);
    }

    public async Task<AttemptResponse> SubmitAnswerAsync(
        int id,
        int questionId,
        int userId,
        SubmitAttemptAnswerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await SubmitAnswerCoreAsync(id, questionId, userId, request, cancellationToken);
        }
        catch (PostgresException exception) when (IsAttemptConcurrencyConflict(exception))
        {
            throw new ConflictException("The attempt was changed concurrently. Try again.");
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The attempt was changed concurrently. Try again.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgresException &&
                                                  IsAttemptConcurrencyConflict(postgresException))
        {
            throw new ConflictException("The attempt was changed concurrently. Try again.");
        }
    }

    private async Task<AttemptResponse> SubmitAnswerCoreAsync(
        int id,
        int questionId,
        int userId,
        SubmitAttemptAnswerRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SelectedOptionPosition <= 0)
        {
            throw new ValidationException("Selected option position must be positive.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var attempt = await GetForUpdateAsync(id, userId, cancellationToken)
            ?? throw new NotFoundException("Attempt was not found.");

        var now = DateTimeOffset.UtcNow;
        if (IsExpired(attempt, now))
        {
            MarkTimedOut(attempt);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            throw new ConflictException("The attempt time limit has expired.");
        }

        if (attempt.Status != AttemptStatus.InProgress)
        {
            throw new ConflictException("The attempt is already finished.");
        }

        var question = attempt.AttemptQuestions.SingleOrDefault(x => x.Id == questionId)
            ?? throw new NotFoundException("The question does not belong to this attempt.");
        if (question.AnsweredAt is not null)
        {
            throw new ConflictException("The question has already been answered.");
        }

        var option = question.OptionsSnapshot.SingleOrDefault(
            x => x.Position == request.SelectedOptionPosition);
        if (option is null)
        {
            throw new ValidationException("Selected option does not exist.");
        }

        question.SelectedOptionPosition = option.Position;
        question.IsCorrect = option.IsCorrect;
        question.AnsweredAt = now;
        if (option.IsCorrect)
        {
            attempt.CorrectAnswers++;
        }
        else
        {
            attempt.WrongAnswers++;
        }

        var answeredCount = attempt.AttemptQuestions.Count(x => x.AnsweredAt is not null);
        if (attempt.Kind == AttemptKind.Exam && attempt.WrongAnswers > ExamMaxWrongAnswers)
        {
            attempt.Status = AttemptStatus.Failed;
            attempt.IsPassed = false;
            attempt.FinishedAt = now;
        }
        else if (answeredCount >= attempt.QuestionCount)
        {
            attempt.Status = AttemptStatus.Completed;
            attempt.FinishedAt = now;
            attempt.IsPassed = attempt.Kind == AttemptKind.Exam ? true : null;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToResponse(attempt);
    }

    private async Task<Attempt?> GetForUpdateAsync(
        int id,
        int userId,
        CancellationToken cancellationToken)
    {
        var attempt = await db.Attempts
            .FromSqlInterpolated($"SELECT * FROM attempts WHERE id = {id} AND user_id = {userId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (attempt is null)
        {
            return null;
        }

        await db.AttemptQuestions
            .Where(x => x.AttemptId == attempt.Id)
            .LoadAsync(cancellationToken);
        return attempt;
    }

    private static bool IsExpired(Attempt attempt, DateTimeOffset now) =>
        attempt.Kind == AttemptKind.Exam &&
        attempt.Status == AttemptStatus.InProgress &&
        attempt.TimeLimitSeconds is int limit &&
        now >= attempt.StartedAt.AddSeconds(limit);

    private static void MarkTimedOut(Attempt attempt)
    {
        attempt.Status = AttemptStatus.TimedOut;
        attempt.IsPassed = false;
        attempt.FinishedAt = attempt.StartedAt.AddSeconds(attempt.TimeLimitSeconds!.Value);
    }

    private async Task<AttemptResponse> StartAttemptAsync(
        int userId,
        int sourceId,
        AttemptKind kind,
        AttemptSource source,
        Func<int, CancellationToken, Task<List<PreparedQuestion>>> getQuestions,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var user = await db.Users.AsNoTracking()
                .Include(x => x.Group)
                .SingleOrDefaultAsync(
                    x => x.Id == userId && x.IsActive && x.Role == UserRole.Cashier,
                    cancellationToken);
            if (user is null)
            {
                throw new ForbiddenException("Only an active cashier can start an attempt.");
            }

            var preparedQuestions = await getQuestions(sourceId, cancellationToken);

            ValidatePreparedQuestions(kind, preparedQuestions);

            var attempt = new Attempt
            {
                UserId = user.Id,
                Kind = kind,
                Source = source,
                TicketId = source == AttemptSource.Prepared && kind == AttemptKind.Training ? sourceId : null,
                ExamId = source == AttemptSource.Prepared && kind == AttemptKind.Exam ? sourceId : null,
                GroupIdSnapshot = user.GroupId,
                GroupNameSnapshot = user.Group?.Name,
                StartedAt = DateTimeOffset.UtcNow,
                FinishedAt = null,
                Status = AttemptStatus.InProgress,
                CorrectAnswers = 0,
                WrongAnswers = 0,
                IsPassed = null,
                QuestionCount = preparedQuestions.Count,
                TimeLimitSeconds = kind == AttemptKind.Exam ? ExamTimeLimitSeconds : null
            };

            foreach (var preparedQuestion in preparedQuestions)
            {
                attempt.AttemptQuestions.Add(new AttemptQuestion
                {
                    QuestionId = preparedQuestion.QuestionId,
                    Position = preparedQuestion.Position,
                    QuestionText = preparedQuestion.Text,
                    OptionsSnapshot = preparedQuestion.Options
                        .OrderBy(x => x.Position)
                        .Select(x => new AnswerOptionSnapshot
                        {
                            Position = x.Position,
                            Text = x.Text,
                            IsCorrect = x.IsCorrect
                        })
                        .ToList(),
                    SelectedOptionPosition = null,
                    IsCorrect = null,
                    AnsweredAt = null
                });
            }

            db.Attempts.Add(attempt);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ToResponse(attempt);
        }
        catch (PostgresException exception) when (IsConcurrencyConflict(exception))
        {
            throw new ConflictException("The question bank was changed concurrently. Try again.");
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The question bank was changed concurrently. Try again.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgresException &&
                                                  IsConcurrencyConflict(postgresException))
        {
            throw new ConflictException("The question bank was changed concurrently. Try again.");
        }
    }

    private async Task<List<PreparedQuestion>> GetRandomTrainingQuestionsAsync(
        int topicId,
        int questionCount,
        CancellationToken cancellationToken)
    {
        var topicExists = await db.Topics
            .FromSqlInterpolated($"SELECT * FROM topics WHERE id = {topicId} FOR SHARE")
            .AsNoTracking()
            .AnyAsync(cancellationToken);
        if (!topicExists)
        {
            throw new NotFoundException("Topic was not found.");
        }

        var availableCount = await db.Questions
            .CountAsync(x => x.TopicId == topicId, cancellationToken);
        if (questionCount > availableCount)
        {
            throw new ValidationException("The topic does not contain enough questions.");
        }

        var questions = await db.Questions.AsNoTracking()
            .Where(x => x.TopicId == topicId)
            .OrderBy(_ => EF.Functions.Random())
            .Take(questionCount)
            .Select(x => new PreparedQuestion(
                x.Id,
                0,
                x.Text,
                x.AnswerOptions
                    .OrderBy(option => option.Position)
                    .Select(option => new PreparedOption(
                        option.Position,
                        option.Text,
                        option.IsCorrect))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return EnsureRandomSelection(questions, questionCount);
    }

    private async Task<List<PreparedQuestion>> GetRandomExamQuestionsAsync(
        CancellationToken cancellationToken)
    {
        var availableCount = await db.Questions.CountAsync(cancellationToken);
        if (availableCount < ExamQuestionCount)
        {
            throw new ValidationException($"The question bank must contain at least {ExamQuestionCount} questions.");
        }

        var questions = await db.Questions.AsNoTracking()
            .OrderBy(_ => EF.Functions.Random())
            .Take(ExamQuestionCount)
            .Select(x => new PreparedQuestion(
                x.Id,
                0,
                x.Text,
                x.AnswerOptions
                    .OrderBy(option => option.Position)
                    .Select(option => new PreparedOption(
                        option.Position,
                        option.Text,
                        option.IsCorrect))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return EnsureRandomSelection(questions, ExamQuestionCount);
    }

    private static List<PreparedQuestion> EnsureRandomSelection(
        List<PreparedQuestion> questions,
        int expectedCount)
    {
        if (questions.Count != expectedCount)
        {
            throw new ConflictException("The question bank was changed concurrently. Try again.");
        }

        return questions
            .Select((question, index) => question with { Position = index + 1 })
            .ToList();
    }

    private async Task<List<PreparedQuestion>> GetTicketQuestionsAsync(
        int ticketId,
        CancellationToken cancellationToken)
    {
        var ticketExists = await db.Tickets
            .FromSqlInterpolated($"SELECT id, title, description, created_at FROM tickets WHERE id = {ticketId} FOR SHARE")
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);
        if (ticketExists is null)
        {
            throw new NotFoundException("Ticket was not found.");
        }

        return await db.TicketQuestions.AsNoTracking()
            .Where(x => x.TicketId == ticketId)
            .OrderBy(x => x.Position)
            .Select(x => new PreparedQuestion(
                x.QuestionId,
                x.Position,
                x.Question.Text,
                x.Question.AnswerOptions
                    .OrderBy(option => option.Position)
                    .Select(option => new PreparedOption(
                        option.Position,
                        option.Text,
                        option.IsCorrect))
                    .ToList()))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<PreparedQuestion>> GetExamQuestionsAsync(
        int examId,
        CancellationToken cancellationToken)
    {
        var examExists = await db.Exams
            .FromSqlInterpolated($"SELECT id, title, description, created_at FROM exams WHERE id = {examId} FOR SHARE")
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);
        if (examExists is null)
        {
            throw new NotFoundException("Exam was not found.");
        }

        return await db.ExamQuestions.AsNoTracking()
            .Where(x => x.ExamId == examId)
            .OrderBy(x => x.Position)
            .Select(x => new PreparedQuestion(
                x.QuestionId,
                x.Position,
                x.Question.Text,
                x.Question.AnswerOptions
                    .OrderBy(option => option.Position)
                    .Select(option => new PreparedOption(
                        option.Position,
                        option.Text,
                        option.IsCorrect))
                    .ToList()))
            .ToListAsync(cancellationToken);
    }

    private static void ValidatePreparedQuestions(
        AttemptKind kind,
        IReadOnlyList<PreparedQuestion> questions)
    {
        if (kind == AttemptKind.Training && questions.Count == 0)
        {
            throw new ValidationException("The ticket does not contain any questions.");
        }

        if (kind == AttemptKind.Exam && questions.Count != ExamQuestionCount)
        {
            throw new ValidationException($"The exam must contain exactly {ExamQuestionCount} questions.");
        }

        if (questions.Select(x => x.Position).Distinct().Count() != questions.Count ||
            questions.Select(x => x.QuestionId).Distinct().Count() != questions.Count)
        {
            throw new ValidationException("The prepared question set is invalid.");
        }

        foreach (var question in questions)
        {
            if (question.Position <= 0 ||
                question.Options.Count < 2 ||
                question.Options.Any(x => x.Position <= 0) ||
                question.Options.Select(x => x.Position).Distinct().Count() != question.Options.Count ||
                question.Options.Count(x => x.IsCorrect) != 1)
            {
                throw new ValidationException("The prepared question set contains an invalid question.");
            }
        }
    }

    private static bool IsConcurrencyConflict(PostgresException exception) =>
        exception.SqlState is PostgresErrorCodes.SerializationFailure
            or PostgresErrorCodes.DeadlockDetected
            or PostgresErrorCodes.ForeignKeyViolation;

    private static bool IsAttemptConcurrencyConflict(PostgresException exception) =>
        exception.SqlState is PostgresErrorCodes.SerializationFailure
            or PostgresErrorCodes.DeadlockDetected;

    private static AttemptResponse ToResponse(Attempt attempt) =>
        new(
            attempt.Id,
            attempt.Kind,
            attempt.Source,
            attempt.Status,
            attempt.StartedAt,
            attempt.FinishedAt,
            attempt.QuestionCount,
            attempt.TimeLimitSeconds,
            attempt.CorrectAnswers,
            attempt.WrongAnswers,
            attempt.IsPassed,
            attempt.AttemptQuestions
                .OrderBy(x => x.Position)
                .Select(question =>
                {
                    var showCorrectAnswers = question.AnsweredAt is not null;
                    return new AttemptQuestionResponse(
                        question.Id,
                        question.QuestionId,
                        question.Position,
                        question.QuestionText,
                        question.OptionsSnapshot
                            .OrderBy(x => x.Position)
                            .Select(option => new AttemptOptionResponse(
                                option.Position,
                                option.Text,
                                showCorrectAnswers ? option.IsCorrect : null))
                            .ToArray(),
                        question.SelectedOptionPosition,
                        showCorrectAnswers ? question.IsCorrect : null,
                        question.AnsweredAt);
                })
                .ToArray());

    private sealed record PreparedQuestion(
        int QuestionId,
        int Position,
        string Text,
        IReadOnlyList<PreparedOption> Options);

    private sealed record PreparedOption(int Position, string Text, bool IsCorrect);
}
