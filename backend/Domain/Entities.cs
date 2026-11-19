namespace Ticketeer.Domain;

public sealed class Group
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ICollection<User> Users { get; } = new List<User>();
}

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

public sealed class LoginEvent
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public User User { get; set; } = null!;
}

public sealed class Topic
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ICollection<Question> Questions { get; } = new List<Question>();
}

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

public sealed class AnswerOption
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public required string Text { get; set; }
    public int Position { get; set; }
    public bool IsCorrect { get; set; }
    public Question Question { get; set; } = null!;
}

public sealed class Ticket
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<TicketQuestion> TicketQuestions { get; } = new List<TicketQuestion>();
    public ICollection<Attempt> Attempts { get; } = new List<Attempt>();
}

public sealed class TicketQuestion
{
    public int TicketId { get; set; }
    public int QuestionId { get; set; }
    public int Position { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public Question Question { get; set; } = null!;
}

public sealed class Exam
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<ExamQuestion> ExamQuestions { get; } = new List<ExamQuestion>();
    public ICollection<Attempt> Attempts { get; } = new List<Attempt>();
}

public sealed class ExamQuestion
{
    public int ExamId { get; set; }
    public int QuestionId { get; set; }
    public int Position { get; set; }
    public Exam Exam { get; set; } = null!;
    public Question Question { get; set; } = null!;
}

public sealed class Attempt
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public AttemptKind Kind { get; set; }
    public AttemptSource Source { get; set; }
    public int? TicketId { get; set; }
    public int? ExamId { get; set; }
    public int? GroupIdSnapshot { get; set; }
    public string? GroupNameSnapshot { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public AttemptStatus Status { get; set; }
    public int CorrectAnswers { get; set; }
    public int WrongAnswers { get; set; }
    public bool? IsPassed { get; set; }
    public int QuestionCount { get; set; }
    public int? TimeLimitSeconds { get; set; }
    public User User { get; set; } = null!;
    public Ticket? Ticket { get; set; }
    public Exam? Exam { get; set; }
    public ICollection<AttemptQuestion> AttemptQuestions { get; } = new List<AttemptQuestion>();
}

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

public sealed class AnswerOptionSnapshot
{
    public int Position { get; set; }
    public required string Text { get; set; }
    public bool IsCorrect { get; set; }
}
