namespace Ticketeer.DTOs;

public sealed record CatalogTopicResponse(int Id, string Name, string? Description, int QuestionCount);

public sealed record CatalogTicketListResponse(
    int Id,
    string Title,
    string? Description,
    int QuestionCount,
    bool IsCompleted);

public sealed record CatalogTicketResponse(
    int Id,
    string Title,
    string? Description,
    int QuestionCount,
    bool IsCompleted,
    IReadOnlyList<CatalogTopicReference> Topics);

public sealed record CatalogTopicReference(int Id, string Name);

public sealed record CatalogExamListResponse(
    int Id,
    string Title,
    string? Description,
    int QuestionCount,
    int TimeLimitSeconds,
    int MaxWrongAnswers);

public sealed record CatalogExamResponse(
    int Id,
    string Title,
    string? Description,
    int QuestionCount,
    int TimeLimitSeconds,
    int MaxWrongAnswers);

public sealed record CatalogQuestionListResponse(int Id, int TopicId, string TopicName, string Text);

public sealed record CatalogQuestionResponse(
    int Id,
    int TopicId,
    string TopicName,
    string Text,
    IReadOnlyList<CatalogAnswerOptionResponse> AnswerOptions);

public sealed record CatalogAnswerOptionResponse(int Position, string Text);
