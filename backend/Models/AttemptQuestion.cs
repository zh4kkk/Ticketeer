namespace Ticketeer.Models;

public sealed class AttemptQuestion
{
    public int Id { get; set; }
    public int AttemptId { get; set; }
    public int? QuestionId { get; set; }
    public int Position { get; set; }
    public required string QuestionText { get; set; }
    public List<AnswerOptionSnapshot> OptionsSnapshot { get; set; } = new();
    public int? SelectedOptionPosition { get; set; }
    public bool? IsCorrect { get; set; }
    public DateTimeOffset? AnsweredAt { get; set; }
    public Attempt Attempt { get; set; } = null!;
    public Question? Question { get; set; }
}
