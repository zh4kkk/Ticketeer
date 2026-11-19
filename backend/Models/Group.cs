namespace Ticketeer.Models;

public sealed class Group
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ICollection<User> Users { get; } = new List<User>();
}
