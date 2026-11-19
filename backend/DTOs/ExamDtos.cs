using System.ComponentModel.DataAnnotations;

namespace Ticketeer.DTOs;

public sealed class ExamRequest
{
    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    [Required]
    [MinLength(10)]
    public List<int> QuestionIds { get; set; } = new();
}

public sealed record ExamListResponse(
    int Id,
    string Title,
    string? Description,
    DateTimeOffset CreatedAt,
    int QuestionCount);

public sealed record ExamQuestionResponse(
    int Id,
    int Position,
    int TopicId,
    string TopicName,
    string Text);

public sealed record ExamResponse(
    int Id,
    string Title,
    string? Description,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ExamQuestionResponse> Questions);
