using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ticketeer.Data;
using Ticketeer.DTOs;
using Ticketeer.Models;

namespace Ticketeer.Services;

public sealed class ExamService(AppDbContext db)
{
    public async Task<IReadOnlyList<ExamListResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await db.Exams.AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new ExamListResponse(
                x.Id,
                x.Title,
                x.Description,
                x.CreatedAt,
                x.ExamQuestions.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<ExamResponse?> GetAsync(int id, CancellationToken cancellationToken)
    {
        var exam = await db.Exams.AsNoTracking()
            .Include(x => x.ExamQuestions)
                .ThenInclude(x => x.Question)
                    .ThenInclude(x => x.Topic)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return exam is null ? null : ToResponse(exam);
    }

    public async Task<ExamResponse> CreateAsync(ExamRequest request, CancellationToken cancellationToken)
    {
        var title = NormalizeTitle(request.Title);
        var description = NormalizeDescription(request.Description);
        ValidateRequest(title, description, request.QuestionIds);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var questions = await GetQuestionsAsync(request.QuestionIds, cancellationToken);
            var exam = new Exam
            {
                Title = title,
                Description = description,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Exams.Add(exam);
            AddExamQuestions(exam, questions, request.QuestionIds);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ToResponse(exam);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("The exam question list conflicts with an existing database constraint.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            throw new ConflictException("A question used by the exam was removed concurrently.");
        }
    }

    public async Task<ExamResponse?> UpdateAsync(int id, ExamRequest request, CancellationToken cancellationToken)
    {
        var title = NormalizeTitle(request.Title);
        var description = NormalizeDescription(request.Description);
        ValidateRequest(title, description, request.QuestionIds);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var exam = await db.Exams
            .FromSqlInterpolated($"SELECT id, title, description, created_at FROM exams WHERE id = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (exam is null)
        {
            return null;
        }

        try
        {
            await db.ExamQuestions
                .Where(x => x.ExamId == id)
                .Include(x => x.Question)
                    .ThenInclude(x => x.Topic)
                .LoadAsync(cancellationToken);
            var questions = await GetQuestionsAsync(request.QuestionIds, cancellationToken);
            var existingQuestions = exam.ExamQuestions.ToArray();
            db.ExamQuestions.RemoveRange(existingQuestions);
            exam.ExamQuestions.Clear();
            await db.SaveChangesAsync(cancellationToken);

            exam.Title = title;
            exam.Description = description;
            AddExamQuestions(exam, questions, request.QuestionIds);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ToResponse(exam);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("The exam question list conflicts with an existing database constraint.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            throw new ConflictException("A question used by the exam was removed concurrently.");
        }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var exam = await db.Exams
            .FromSqlInterpolated($"SELECT id, title, description, created_at FROM exams WHERE id = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (exam is null)
        {
            return false;
        }

        db.Exams.Remove(exam);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<List<Question>> GetQuestionsAsync(IReadOnlyList<int> questionIds, CancellationToken cancellationToken)
    {
        var questions = await db.Questions
            .Include(x => x.Topic)
            .Where(x => questionIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
        var questionsById = questions.ToDictionary(x => x.Id);
        var missingId = questionIds.FirstOrDefault(id => !questionsById.ContainsKey(id));
        if (missingId != 0)
        {
            throw new NotFoundException("One or more questions were not found.");
        }

        return questions;
    }

    private static void AddExamQuestions(Exam exam, IReadOnlyList<Question> questions, IReadOnlyList<int> questionIds)
    {
        var questionsById = questions.ToDictionary(x => x.Id);
        for (var index = 0; index < questionIds.Count; index++)
        {
            exam.ExamQuestions.Add(new ExamQuestion
            {
                Exam = exam,
                Question = questionsById[questionIds[index]],
                Position = index + 1
            });
        }
    }

    private static ExamResponse ToResponse(Exam exam) =>
        new(
            exam.Id,
            exam.Title,
            exam.Description,
            exam.CreatedAt,
            exam.ExamQuestions
                .OrderBy(x => x.Position)
                .Select(x => new ExamQuestionResponse(
                    x.Question.Id,
                    x.Position,
                    x.Question.TopicId,
                    x.Question.Topic.Name,
                    x.Question.Text))
                .ToArray());

    private static string NormalizeTitle(string title) => title.Trim();

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static void ValidateRequest(string title, string? description, IReadOnlyList<int>? questionIds)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("Exam title is required.");
        }

        if (title.Length > 300)
        {
            throw new ValidationException("Exam title cannot exceed 300 characters.");
        }

        if (description?.Length > 4000)
        {
            throw new ValidationException("Exam description cannot exceed 4000 characters.");
        }

        if (questionIds is null || questionIds.Count != 10)
        {
            throw new ValidationException("An exam must contain exactly 10 questions.");
        }

        if (questionIds.Any(id => id <= 0))
        {
            throw new ValidationException("Question identifiers must be positive.");
        }

        if (questionIds.Distinct().Count() != questionIds.Count)
        {
            throw new ValidationException("A question cannot be repeated in an exam.");
        }
    }
}
