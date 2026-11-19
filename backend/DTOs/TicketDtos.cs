using System.ComponentModel.DataAnnotations;

namespace Ticketeer.DTOs;

public sealed class TicketRequest
{
    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    [Required]
    [MinLength(1)]
    public List<int> QuestionIds { get; set; } = new();
}

public sealed record TicketListResponse(
    int Id,
    string Title,
    string? Description,
    DateTimeOffset CreatedAt,
    int QuestionCount);

public sealed record TicketQuestionResponse(
    int Id,
    int Position,
    int TopicId,
    string TopicName,
    string Text);

public sealed record TicketResponse(
    int Id,
    string Title,
    string? Description,
    DateTimeOffset CreatedAt,
    IReadOnlyList<TicketQuestionResponse> Questions);
