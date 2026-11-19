namespace Ticketeer.Models;

public sealed class AnswerOptionSnapshot
{
    public int Position { get; set; }
    public required string Text { get; set; }
    public bool IsCorrect { get; set; }
}
