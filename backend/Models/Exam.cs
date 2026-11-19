namespace Ticketeer.Models;

public sealed class Exam
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<ExamQuestion> ExamQuestions { get; } = new List<ExamQuestion>();
    public ICollection<Attempt> Attempts { get; } = new List<Attempt>();
}
