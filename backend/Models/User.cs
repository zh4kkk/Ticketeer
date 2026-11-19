namespace Ticketeer.Models;

public sealed class User
{
    public int Id { get; set; }
    public required string Login { get; set; }
    public required string NormalizedLogin { get; set; }
    public required string PasswordHash { get; set; }
    public required string FullName { get; set; }
    public UserRole Role { get; set; }
    public int? GroupId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public Group? Group { get; set; }
    public ICollection<LoginEvent> LoginEvents { get; } = new List<LoginEvent>();
    public ICollection<Attempt> Attempts { get; } = new List<Attempt>();
}
