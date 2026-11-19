using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ticketeer.Data;
using Ticketeer.DTOs;
using Ticketeer.Models;

namespace Ticketeer.Services;

public sealed class QuestionService(AppDbContext db)
{
    public async Task<IReadOnlyList<QuestionListResponse>> GetAllAsync(int? topicId, CancellationToken cancellationToken)
    {
        var query = db.Questions.AsNoTracking().AsQueryable();
        if (topicId is not null)
        {
            query = query.Where(x => x.TopicId == topicId.Value);
        }

        return await query
            .OrderBy(x => x.Id)
            .Select(x => new QuestionListResponse(
                x.Id,
                x.TopicId,
                x.Topic.Name,
                x.Text,
                x.CreatedAt,
                x.AnswerOptions.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<QuestionResponse?> GetAsync(int id, CancellationToken cancellationToken)
    {
        var question = await db.Questions.AsNoTracking()
            .Include(x => x.Topic)
            .Include(x => x.AnswerOptions)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return question is null ? null : ToResponse(question);
    }

    public async Task<QuestionResponse> CreateAsync(QuestionRequest request, CancellationToken cancellationToken)
    {
        var text = NormalizeText(request.Text);
        ValidateRequest(text, request.AnswerOptions);
        var topic = await GetTopicAsync(request.TopicId, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var question = new Question
            {
                TopicId = topic.Id,
                Topic = topic,
                Text = text,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Questions.Add(question);
            await db.SaveChangesAsync(cancellationToken);

            AddAnswerOptions(question, request.AnswerOptions);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ToResponse(question);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("Question answer options conflict with an existing database constraint.");
        }
    }

    public async Task<QuestionResponse?> UpdateAsync(int id, QuestionRequest request, CancellationToken cancellationToken)
    {
        var question = await db.Questions
            .Include(x => x.Topic)
            .Include(x => x.AnswerOptions)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (question is null)
        {
            return null;
        }

        var text = NormalizeText(request.Text);
        ValidateRequest(text, request.AnswerOptions);
        var topic = await GetTopicAsync(request.TopicId, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var existingOptions = question.AnswerOptions.ToArray();
            db.AnswerOptions.RemoveRange(existingOptions);
            question.AnswerOptions.Clear();
            await db.SaveChangesAsync(cancellationToken);

            question.TopicId = topic.Id;
            question.Topic = topic;
            question.Text = text;
            AddAnswerOptions(question, request.AnswerOptions);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ToResponse(question);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("Question answer options conflict with an existing database constraint.");
        }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var question = await db.Questions.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (question is null)
        {
            return false;
        }

        if (await db.TicketQuestions.AnyAsync(x => x.QuestionId == id, cancellationToken) ||
            await db.ExamQuestions.AnyAsync(x => x.QuestionId == id, cancellationToken))
        {
            throw new ConflictException("The question cannot be deleted while it is used in a ticket or exam.");
        }

        db.Questions.Remove(question);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            throw new ConflictException("The question cannot be deleted while it is used in a ticket or exam.");
        }

        return true;
    }

    private async Task<Topic> GetTopicAsync(int topicId, CancellationToken cancellationToken)
    {
        var topic = await db.Topics.SingleOrDefaultAsync(x => x.Id == topicId, cancellationToken);
        return topic ?? throw new NotFoundException("Topic was not found.");
    }

    private static void AddAnswerOptions(Question question, IReadOnlyList<AnswerOptionRequest> options)
    {
        for (var index = 0; index < options.Count; index++)
        {
            question.AnswerOptions.Add(new AnswerOption
            {
                Question = question,
                Text = NormalizeText(options[index].Text),
                Position = index + 1,
                IsCorrect = options[index].IsCorrect
            });
        }
    }

    private static QuestionResponse ToResponse(Question question) =>
        new(
            question.Id,
            question.TopicId,
            question.Topic.Name,
            question.Text,
            question.CreatedAt,
            question.AnswerOptions
                .OrderBy(x => x.Position)
                .Select(x => new AnswerOptionResponse(x.Id, x.Position, x.Text, x.IsCorrect))
                .ToArray());

    private static string NormalizeText(string text) => text.Trim();

    private static void ValidateRequest(string text, IReadOnlyList<AnswerOptionRequest>? options)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ValidationException("Question text is required.");
        }

        if (text.Length > 10000)
        {
            throw new ValidationException("Question text cannot exceed 10000 characters.");
        }

        if (options is null || options.Count < 2)
        {
            throw new ValidationException("A question must contain at least two answer options.");
        }

        if (options.Count(x => x.IsCorrect) != 1)
        {
            throw new ValidationException("A question must contain exactly one correct answer.");
        }

        foreach (var option in options)
        {
            var optionText = NormalizeText(option.Text);
            if (string.IsNullOrWhiteSpace(optionText))
            {
                throw new ValidationException("Answer option text is required.");
            }

            if (optionText.Length > 2000)
            {
                throw new ValidationException("Answer option text cannot exceed 2000 characters.");
            }
        }
    }
}
