using System.ComponentModel.DataAnnotations;

namespace Ticketeer.DTOs;

public sealed class QuestionRequest
{
    [Range(1, int.MaxValue)]
    public int TopicId { get; set; }

    [Required]
    [MaxLength(10000)]
    public string Text { get; set; } = string.Empty;

    [Required]
    public List<AnswerOptionRequest> AnswerOptions { get; set; } = new();
}

public sealed class AnswerOptionRequest
{
    [Required]
    [MaxLength(2000)]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }
}

public sealed record QuestionListResponse(
    int Id,
    int TopicId,
    string TopicName,
    string Text,
    DateTimeOffset CreatedAt,
    int AnswerOptionCount);

public sealed record AnswerOptionResponse(
    int Id,
    int Position,
    string Text,
    bool IsCorrect);

public sealed record QuestionResponse(
    int Id,
    int TopicId,
    string TopicName,
    string Text,
    DateTimeOffset CreatedAt,
    IReadOnlyList<AnswerOptionResponse> AnswerOptions);
