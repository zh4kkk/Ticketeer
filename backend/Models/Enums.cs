namespace Ticketeer.Models;

public enum UserRole
{
    Cashier,
    Admin
}

public enum AttemptKind
{
    Training,
    Exam
}

public enum AttemptSource
{
    Prepared,
    Random
}

public enum AttemptStatus
{
    InProgress,
    Completed,
    Failed,
    TimedOut
}
