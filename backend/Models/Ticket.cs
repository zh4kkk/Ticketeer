namespace Ticketeer.Models;

public sealed class Ticket
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<TicketQuestion> TicketQuestions { get; } = new List<TicketQuestion>();
    public ICollection<Attempt> Attempts { get; } = new List<Attempt>();
}
