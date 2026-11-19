namespace Ticketeer.Models;

public sealed class AnswerOption
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public required string Text { get; set; }
    public int Position { get; set; }
    public bool IsCorrect { get; set; }
    public Question Question { get; set; } = null!;
}
