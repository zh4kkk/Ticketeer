namespace Ticketeer.Models;

public sealed class Attempt
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public AttemptKind Kind { get; set; }
    public AttemptSource Source { get; set; }
    public int? TicketId { get; set; }
    public int? ExamId { get; set; }
    public int? GroupIdSnapshot { get; set; }
    public string? GroupNameSnapshot { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public AttemptStatus Status { get; set; }
    public int CorrectAnswers { get; set; }
    public int WrongAnswers { get; set; }
    public bool? IsPassed { get; set; }
    public int QuestionCount { get; set; }
    public int? TimeLimitSeconds { get; set; }
    public User User { get; set; } = null!;
    public Ticket? Ticket { get; set; }
    public Exam? Exam { get; set; }
    public ICollection<AttemptQuestion> AttemptQuestions { get; } = new List<AttemptQuestion>();
}
