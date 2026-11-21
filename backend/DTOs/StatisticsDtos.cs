using Ticketeer.Models;

namespace Ticketeer.DTOs;

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record AttemptHistoryItem(
    int Id,
    AttemptKind Kind,
    AttemptSource Source,
    AttemptStatus Status,
    int? TicketId,
    string? TicketTitle,
    int? ExamId,
    string? ExamTitle,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    int QuestionCount,
    int CorrectAnswers,
    int WrongAnswers,
    bool? IsPassed);

public sealed record StatisticsResponse(
    int TotalAttempts,
    int CompletedTrainingCount,
    int FinishedExamCount,
    int PassedExamCount,
    int FailedExamCount,
    int CorrectAnswers,
    int WrongAnswers,
    decimal CorrectAnswerPercentage,
    decimal ExamPassPercentage,
    int TotalPreparedTickets,
    int CompletedPreparedTickets,
    decimal PreparedTicketsPercentage);

public sealed record ActivityResponse(
    DateOnly Date,
    int LoginCount,
    int CompletedTrainingCount,
    int FinishedExamCount);

public sealed record UserStatisticsResponse(
    int UserId,
    string Login,
    string FullName,
    int? GroupId,
    string? GroupName,
    bool IsActive,
    StatisticsResponse Statistics);

public sealed record GroupStatisticsResponse(
    int Id,
    string Name,
    int CurrentEmployeeCount,
    int CurrentActiveEmployeeCount,
    int CompletedTrainingCount,
    int FinishedExamCount,
    int PassedExamCount,
    int FailedExamCount,
    int CorrectAnswers,
    int WrongAnswers,
    decimal CorrectAnswerPercentage,
    decimal ExamPassPercentage);
