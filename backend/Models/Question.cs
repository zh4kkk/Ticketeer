namespace Ticketeer.Models;

public sealed class Question
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public required string Text { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Topic Topic { get; set; } = null!;
    public ICollection<AnswerOption> AnswerOptions { get; } = new List<AnswerOption>();
    public ICollection<TicketQuestion> TicketQuestions { get; } = new List<TicketQuestion>();
    public ICollection<ExamQuestion> ExamQuestions { get; } = new List<ExamQuestion>();
    public ICollection<AttemptQuestion> AttemptQuestions { get; } = new List<AttemptQuestion>();
}
