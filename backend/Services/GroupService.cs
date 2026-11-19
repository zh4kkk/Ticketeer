using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ticketeer.Data;
using Ticketeer.DTOs;
using Ticketeer.Models;

namespace Ticketeer.Services;

public sealed class GroupService(AppDbContext db)
{
    public async Task<IReadOnlyList<GroupResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var groups = await db.Groups.AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
        return groups.Select(ToResponse).ToArray();
    }

    public async Task<GroupResponse?> GetAsync(int id, CancellationToken cancellationToken)
    {
        var group = await db.Groups.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return group is null ? null : ToResponse(group);
    }

    public async Task<GroupResponse> CreateAsync(GroupRequest request, CancellationToken cancellationToken)
    {
        var name = NormalizeName(request.Name);
        ValidateName(name);
        var description = NormalizeDescription(request.Description);
        ValidateDescription(description);

        if (await db.Groups.AnyAsync(x => x.Name == name, cancellationToken))
        {
            throw new ConflictException("A group with this name already exists.");
        }

        var group = new Group
        {
            Name = name,
            Description = description
        };
        db.Groups.Add(group);
        await SaveWithConflictHandlingAsync(cancellationToken);
        return ToResponse(group);
    }

    public async Task<GroupResponse?> UpdateAsync(int id, GroupRequest request, CancellationToken cancellationToken)
    {
        var group = await db.Groups.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (group is null)
        {
            return null;
        }

        var name = NormalizeName(request.Name);
        ValidateName(name);
        var description = NormalizeDescription(request.Description);
        ValidateDescription(description);

        if (await db.Groups.AnyAsync(x => x.Id != id && x.Name == name, cancellationToken))
        {
            throw new ConflictException("A group with this name already exists.");
        }

        group.Name = name;
        group.Description = description;
        await SaveWithConflictHandlingAsync(cancellationToken);
        return ToResponse(group);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var group = await db.Groups.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (group is null)
        {
            return false;
        }

        db.Groups.Remove(group);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static GroupResponse ToResponse(Group group) =>
        new(group.Id, group.Name, group.Description);

    private static string NormalizeName(string name) => name.Trim();

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Group name is required.");
        }

        if (name.Length > 200)
        {
            throw new ValidationException("Group name cannot exceed 200 characters.");
        }
    }

    private static void ValidateDescription(string? description)
    {
        if (description?.Length > 2000)
        {
            throw new ValidationException("Group description cannot exceed 2000 characters.");
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
            throw new ConflictException("A group with this name already exists.");
        }
    }
}
