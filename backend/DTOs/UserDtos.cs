using System.ComponentModel.DataAnnotations;

namespace Ticketeer.DTOs;

public sealed class CreateUserRequest
{
    [Required]
    [MaxLength(100)]
    public string Login { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public int? GroupId { get; set; }
}

public sealed class UpdateUserRequest
{
    [Required]
    [MaxLength(100)]
    public string Login { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    public int? GroupId { get; set; }
    public bool IsActive { get; set; }
}

public sealed class ResetPasswordRequest
{
    [Required]
    public string Password { get; set; } = string.Empty;
}

public sealed record UserResponse(
    int Id,
    string Login,
    string FullName,
    string Role,
    int? GroupId,
    string? GroupName,
    bool IsActive,
    DateTimeOffset CreatedAt);
