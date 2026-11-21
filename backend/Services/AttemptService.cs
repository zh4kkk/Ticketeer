using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ticketeer.Data;
using Ticketeer.DTOs;
using Ticketeer.Models;

namespace Ticketeer.Services;

public sealed class AttemptService(AppDbContext db)
{
    private const int ExamQuestionCount = 10;
    private const int ExamTimeLimitSeconds = 20 * 60;

    public Task<AttemptResponse> StartTrainingTicketAsync(
        int ticketId,
        int userId,
        CancellationToken cancellationToken) =>
        StartPreparedAsync(
            userId,
            ticketId,
            AttemptKind.Training,
            cancellationToken);

    public Task<AttemptResponse> StartExamAsync(
        int examId,
        int userId,
        CancellationToken cancellationToken) =>
        StartPreparedAsync(
            userId,
            examId,
            AttemptKind.Exam,
            cancellationToken);

    public async Task<AttemptResponse?> GetAsync(
        int id,
        int userId,
        CancellationToken cancellationToken)
    {
        var attempt = await db.Attempts.AsNoTracking()
            .Include(x => x.AttemptQuestions)
            .SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

        return attempt is null ? null : ToResponse(attempt);
    }

    private async Task<AttemptResponse> StartPreparedAsync(
        int userId,
        int preparedSetId,
        AttemptKind kind,
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

            var preparedQuestions = kind == AttemptKind.Training
                ? await GetTicketQuestionsAsync(preparedSetId, cancellationToken)
                : await GetExamQuestionsAsync(preparedSetId, cancellationToken);

            ValidatePreparedQuestions(kind, preparedQuestions);

            var attempt = new Attempt
            {
                UserId = user.Id,
                Kind = kind,
                Source = AttemptSource.Prepared,
                TicketId = kind == AttemptKind.Training ? preparedSetId : null,
                ExamId = kind == AttemptKind.Exam ? preparedSetId : null,
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
            throw new ConflictException("The prepared question set was changed concurrently. Try again.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgresException &&
                                                  IsConcurrencyConflict(postgresException))
        {
            throw new ConflictException("The prepared question set was changed concurrently. Try again.");
        }
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
