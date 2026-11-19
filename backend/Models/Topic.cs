namespace Ticketeer.Models;

public sealed class Topic
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ICollection<Question> Questions { get; } = new List<Question>();
}
