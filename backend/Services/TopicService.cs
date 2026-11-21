using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ticketeer.Data;
using Ticketeer.DTOs;
using Ticketeer.Models;

namespace Ticketeer.Services;

public sealed class TopicService(AppDbContext db)
{
    public async Task<IReadOnlyList<TopicResponse>> GetAllAsync(CancellationToken cancellationToken, string? search = null)
    {
        var query = db.Topics.AsNoTracking();
        var pattern = CatalogSearch.Pattern(search);
        if (pattern is not null)
        {
            query = query.Where(x => EF.Functions.ILike(x.Name, pattern, "\\"));
        }

        var topics = await query
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
        return topics.Select(ToResponse).ToArray();
    }

    public async Task<TopicResponse?> GetAsync(int id, CancellationToken cancellationToken)
    {
        var topic = await db.Topics.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return topic is null ? null : ToResponse(topic);
    }

    public async Task<TopicResponse> CreateAsync(TopicRequest request, CancellationToken cancellationToken)
    {
        var name = NormalizeName(request.Name);
        ValidateName(name);
        var description = NormalizeDescription(request.Description);
        ValidateDescription(description);

        if (await db.Topics.AnyAsync(x => x.Name == name, cancellationToken))
        {
            throw new ConflictException("A topic with this name already exists.");
        }

        var topic = new Topic
        {
            Name = name,
            Description = description
        };
        db.Topics.Add(topic);
        await SaveWithConflictHandlingAsync(cancellationToken);
        return ToResponse(topic);
    }

    public async Task<TopicResponse?> UpdateAsync(int id, TopicRequest request, CancellationToken cancellationToken)
    {
        var topic = await db.Topics.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (topic is null)
        {
            return null;
        }

        var name = NormalizeName(request.Name);
        ValidateName(name);
        var description = NormalizeDescription(request.Description);
        ValidateDescription(description);

        if (await db.Topics.AnyAsync(x => x.Id != id && x.Name == name, cancellationToken))
        {
            throw new ConflictException("A topic with this name already exists.");
        }

        topic.Name = name;
        topic.Description = description;
        await SaveWithConflictHandlingAsync(cancellationToken);
        return ToResponse(topic);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var topic = await db.Topics.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (topic is null)
        {
            return false;
        }

        if (await db.Questions.AnyAsync(x => x.TopicId == id, cancellationToken))
        {
            throw new ConflictException("The topic cannot be deleted while it contains questions.");
        }

        db.Topics.Remove(topic);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            throw new ConflictException("The topic cannot be deleted while it contains questions.");
        }

        return true;
    }

    private static TopicResponse ToResponse(Topic topic) =>
        new(topic.Id, topic.Name, topic.Description);

    private static string NormalizeName(string name) => name.Trim();

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Topic name is required.");
        }

        if (name.Length > 200)
        {
            throw new ValidationException("Topic name cannot exceed 200 characters.");
        }
    }

    private static void ValidateDescription(string? description)
    {
        if (description?.Length > 2000)
        {
            throw new ValidationException("Topic description cannot exceed 2000 characters.");
        }
    }

    private async Task SaveWithConflictHandlingAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("A topic with this name already exists.");
        }
    }
}
