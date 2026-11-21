using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ticketeer.Data;
using Ticketeer.DTOs;
using Ticketeer.Models;

namespace Ticketeer.Services;

public sealed class TicketService(AppDbContext db)
{
    public async Task<IReadOnlyList<TicketListResponse>> GetAllAsync(CancellationToken cancellationToken, string? search = null, int? topicId = null)
    {
        var query = db.Tickets.AsNoTracking().AsQueryable();
        var pattern = CatalogSearch.Pattern(search);
        if (pattern is not null)
        {
            query = query.Where(x => EF.Functions.ILike(x.Title, pattern, "\\") ||
                                     (x.Description != null && EF.Functions.ILike(x.Description, pattern, "\\")));
        }
        if (topicId is not null)
        {
            query = query.Where(x => x.TicketQuestions.Any(question => question.Question.TopicId == topicId.Value));
        }

        return await query
            .OrderBy(x => x.Id)
            .Select(x => new TicketListResponse(
                x.Id,
                x.Title,
                x.Description,
                x.CreatedAt,
                x.TicketQuestions.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<TicketResponse?> GetAsync(int id, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets.AsNoTracking()
            .Include(x => x.TicketQuestions)
                .ThenInclude(x => x.Question)
                    .ThenInclude(x => x.Topic)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return ticket is null ? null : ToResponse(ticket);
    }

    public async Task<TicketResponse> CreateAsync(TicketRequest request, CancellationToken cancellationToken)
    {
        var title = NormalizeTitle(request.Title);
        var description = NormalizeDescription(request.Description);
        ValidateRequest(title, description, request.QuestionIds);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var questions = await GetQuestionsAsync(request.QuestionIds, cancellationToken);
            var ticket = new Ticket
            {
                Title = title,
                Description = description,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Tickets.Add(ticket);

            AddTicketQuestions(ticket, questions, request.QuestionIds);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ToResponse(ticket);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("The ticket question list conflicts with an existing database constraint.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            throw new ConflictException("A question used by the ticket was removed concurrently.");
        }
    }

    public async Task<TicketResponse?> UpdateAsync(int id, TicketRequest request, CancellationToken cancellationToken)
    {
        var title = NormalizeTitle(request.Title);
        var description = NormalizeDescription(request.Description);
        ValidateRequest(title, description, request.QuestionIds);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var ticket = await db.Tickets
            .FromSqlInterpolated($"SELECT id, title, description, created_at FROM tickets WHERE id = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (ticket is null)
        {
            return null;
        }

        try
        {
            await db.TicketQuestions
                .Where(x => x.TicketId == id)
                .Include(x => x.Question)
                    .ThenInclude(x => x.Topic)
                .LoadAsync(cancellationToken);
            var questions = await GetQuestionsAsync(request.QuestionIds, cancellationToken);
            var existingQuestions = ticket.TicketQuestions.ToArray();
            db.TicketQuestions.RemoveRange(existingQuestions);
            ticket.TicketQuestions.Clear();
            await db.SaveChangesAsync(cancellationToken);

            ticket.Title = title;
            ticket.Description = description;
            AddTicketQuestions(ticket, questions, request.QuestionIds);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ToResponse(ticket);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("The ticket question list conflicts with an existing database constraint.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            throw new ConflictException("A question used by the ticket was removed concurrently.");
        }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var ticket = await db.Tickets
            .FromSqlInterpolated($"SELECT id, title, description, created_at FROM tickets WHERE id = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (ticket is null)
        {
            return false;
        }

        db.Tickets.Remove(ticket);
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

    private static void AddTicketQuestions(Ticket ticket, IReadOnlyList<Question> questions, IReadOnlyList<int> questionIds)
    {
        var questionsById = questions.ToDictionary(x => x.Id);
        for (var index = 0; index < questionIds.Count; index++)
        {
            ticket.TicketQuestions.Add(new TicketQuestion
            {
                Ticket = ticket,
                Question = questionsById[questionIds[index]],
                Position = index + 1
            });
        }
    }

    private static TicketResponse ToResponse(Ticket ticket) =>
        new(
            ticket.Id,
            ticket.Title,
            ticket.Description,
            ticket.CreatedAt,
            ticket.TicketQuestions
                .OrderBy(x => x.Position)
                .Select(x => new TicketQuestionResponse(
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
            throw new ValidationException("Ticket title is required.");
        }

        if (title.Length > 300)
        {
            throw new ValidationException("Ticket title cannot exceed 300 characters.");
        }

        if (description?.Length > 4000)
        {
            throw new ValidationException("Ticket description cannot exceed 4000 characters.");
        }

        if (questionIds is null || questionIds.Count == 0)
        {
            throw new ValidationException("A ticket must contain at least one question.");
        }

        if (questionIds.Any(id => id <= 0))
        {
            throw new ValidationException("Question identifiers must be positive.");
        }

        if (questionIds.Distinct().Count() != questionIds.Count)
        {
            throw new ValidationException("A question cannot be repeated in a ticket.");
        }
    }
}
