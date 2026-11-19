using Microsoft.EntityFrameworkCore;
using Ticketeer.Models;

namespace Ticketeer.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<User> Users => Set<User>();
    public DbSet<LoginEvent> LoginEvents => Set<LoginEvent>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketQuestion> TicketQuestions => Set<TicketQuestion>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamQuestion> ExamQuestions => Set<ExamQuestion>();
    public DbSet<Attempt> Attempts => Set<Attempt>();
    public DbSet<AttemptQuestion> AttemptQuestions => Set<AttemptQuestion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
