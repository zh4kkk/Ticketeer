namespace Ticketeer.Models;

public sealed class ExamQuestion
{
    public int ExamId { get; set; }
    public int QuestionId { get; set; }
    public int Position { get; set; }
    public Exam Exam { get; set; } = null!;
    public Question Question { get; set; } = null!;
}
