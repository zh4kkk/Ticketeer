using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Ticketeer.Data;
using Ticketeer.Models;

namespace Ticketeer.Services;

public sealed class BootstrapAdminService(AppDbContext db, IConfiguration configuration, ILogger<BootstrapAdminService> logger)
{
    public async Task TryCreateAsync(CancellationToken cancellationToken)
    {
        var login = configuration["BootstrapAdmin:Login"];
        var password = configuration["BootstrapAdmin:Password"];
        var fullName = configuration["BootstrapAdmin:FullName"];
        if (login is null && password is null && fullName is null)
        {
            return;
        }

        if (login is null || password is null || fullName is null)
        {
            throw new ValidationException("Bootstrap administrator configuration requires Login, Password and FullName.");
        }

        if (await db.Users.AnyAsync(x => x.Role == UserRole.Admin, cancellationToken))
        {
            return;
        }

        var normalizedLogin = UserService.NormalizeLogin(login);
        UserService.ValidateLogin(login);
        UserService.ValidatePassword(password);
        UserService.ValidateFullName(fullName);

        var admin = new User
        {
            Login = login.Trim(),
            NormalizedLogin = normalizedLogin,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = fullName.Trim(),
            Role = UserRole.Admin,
            GroupId = null,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(admin);
        await db.SaveChangesAsync(cancellationToken);

        Environment.SetEnvironmentVariable("BootstrapAdmin__Login", null);
        Environment.SetEnvironmentVariable("BootstrapAdmin__Password", null);
        Environment.SetEnvironmentVariable("BootstrapAdmin__FullName", null);
        logger.LogInformation("Bootstrap administrator created for login {Login}.", admin.Login);
    }
}
