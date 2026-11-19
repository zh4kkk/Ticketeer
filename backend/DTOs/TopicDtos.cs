using System.ComponentModel.DataAnnotations;

namespace Ticketeer.DTOs;

public sealed class TopicRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }
}

public sealed record TopicResponse(
    int Id,
    string Name,
    string? Description);
