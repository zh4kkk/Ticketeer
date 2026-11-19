using System.ComponentModel.DataAnnotations;

namespace Ticketeer.DTOs;

public sealed class LoginRequest
{
    [Required]
    [MaxLength(100)]
    public string Login { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public sealed record LoginResponse(string Token, DateTimeOffset ExpiresAt);

public sealed record CurrentUserResponse(
    int Id,
    string Login,
    string FullName,
    string Role,
    int? GroupId,
    string? GroupName);
