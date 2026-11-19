using BCrypt.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ticketeer.Data;
using Ticketeer.DTOs;
using Ticketeer.Models;

namespace Ticketeer.Services;

public sealed class UserService(AppDbContext db)
{
    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var users = await db.Users.AsNoTracking().Include(x => x.Group)
            .Where(x => x.Role == UserRole.Cashier)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
        return users.Select(ToResponse).ToArray();
    }

    public async Task<UserResponse?> GetAsync(int id, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().Include(x => x.Group)
            .SingleOrDefaultAsync(x => x.Id == id && x.Role == UserRole.Cashier, cancellationToken);
        return user is null ? null : ToResponse(user);
    }

    public async Task<UserResponse> CreateCashierAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var login = NormalizeLogin(request.Login);
        ValidateLoginAndPassword(login, request.Password);
        ValidateFullName(request.FullName);
        var group = await GetGroupAsync(request.GroupId, cancellationToken);
        var user = new User
        {
            Login = request.Login.Trim(),
            NormalizedLogin = login,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            Role = UserRole.Cashier,
            GroupId = group?.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            IsActive = true
        };

        db.Users.Add(user);
        await SaveWithConflictHandlingAsync(cancellationToken);
        user.Group = group;
        return ToResponse(user);
    }

    public async Task<UserResponse?> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.Include(x => x.Group).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        if (user.Role == UserRole.Admin)
        {
            if (request.IsActive == false && user.IsActive && await db.Users.CountAsync(x => x.Role == UserRole.Admin && x.IsActive, cancellationToken) <= 1)
            {
                throw new ConflictException("The last active administrator cannot be deactivated.");
            }

            throw new ForbiddenException("Administrator accounts cannot be modified through this endpoint.");
        }

        if (request.IsActive is null)
        {
            throw new ValidationException("IsActive is required.");
        }

        var login = NormalizeLogin(request.Login);
        ValidateLogin(login);
        ValidateFullName(request.FullName);

        var group = await GetGroupAsync(request.GroupId, cancellationToken);
        user.Login = request.Login.Trim();
        user.NormalizedLogin = login;
        user.FullName = request.FullName.Trim();
        user.GroupId = group?.Id;
        user.Group = group;
        user.IsActive = request.IsActive.Value;
        await SaveWithConflictHandlingAsync(cancellationToken);
        return ToResponse(user);
    }

    public async Task<bool> ResetPasswordAsync(int id, ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
        {
            return false;
        }

        if (user.Role == UserRole.Admin)
        {
            throw new ForbiddenException("Administrator passwords cannot be reset through this endpoint.");
        }

        ValidatePassword(request.Password);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public static string NormalizeLogin(string login) => login.Trim().ToUpperInvariant();

    public static CurrentUserResponse ToCurrentUserResponse(User user) =>
        new(user.Id, user.Login, user.FullName, user.Role.ToString(), user.GroupId, user.Group?.Name);

    private static UserResponse ToResponse(User user) =>
        new(user.Id, user.Login, user.FullName, user.Role.ToString(), user.GroupId, user.Group?.Name, user.IsActive, user.CreatedAt);

    private async Task<Group?> GetGroupAsync(int? groupId, CancellationToken cancellationToken)
    {
        if (groupId is null)
        {
            return null;
        }

        var group = await db.Groups.SingleOrDefaultAsync(x => x.Id == groupId.Value, cancellationToken);
        return group ?? throw new NotFoundException("Group was not found.");
    }

    private static void ValidateLoginAndPassword(string login, string password)
    {
        ValidateLogin(login);
        ValidatePassword(password);
    }

    public static void ValidateLogin(string login)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            throw new ValidationException("Login is required.");
        }

        if (login.Trim().Length > 100)
        {
            throw new ValidationException("Login cannot exceed 100 characters.");
        }
    }

    public static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException("Password is required and must contain at least 8 characters.");
        }

        if (password.Length < 8)
        {
            throw new ValidationException("Password must contain at least 8 characters.");
        }

        if (Encoding.UTF8.GetByteCount(password) > 72)
        {
            throw new ValidationException("Password cannot exceed 72 UTF-8 bytes because of BCrypt limits.");
        }
    }

    public static void ValidateFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ValidationException("Full name is required.");
        }

        if (fullName.Trim().Length > 200)
        {
            throw new ValidationException("Full name cannot exceed 200 characters.");
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
            throw new ConflictException("A user with this login already exists.");
        }
    }
}
