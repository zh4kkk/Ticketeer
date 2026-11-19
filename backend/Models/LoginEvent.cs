namespace Ticketeer.Models;

public sealed class LoginEvent
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public User User { get; set; } = null!;
}
