namespace Ticketeer.Models;

public sealed class TicketQuestion
{
    public int TicketId { get; set; }
    public int QuestionId { get; set; }
    public int Position { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public Question Question { get; set; } = null!;
}
