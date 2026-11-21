using System.Text.Json.Serialization;
using Ticketeer.Models;

namespace Ticketeer.DTOs;

public sealed record AttemptOptionResponse(
    int Position,
    string Text,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsCorrect);

public sealed record AttemptQuestionResponse(
    int Id,
    int? QuestionId,
    int Position,
    string QuestionText,
    IReadOnlyList<AttemptOptionResponse> Options,
    int? SelectedOptionPosition,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsCorrect,
    DateTimeOffset? AnsweredAt);

public sealed record AttemptResponse(
    int Id,
    AttemptKind Kind,
    AttemptSource Source,
    AttemptStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    int QuestionCount,
    int? TimeLimitSeconds,
    int CorrectAnswers,
    int WrongAnswers,
    bool? IsPassed,
    IReadOnlyList<AttemptQuestionResponse> Questions);
