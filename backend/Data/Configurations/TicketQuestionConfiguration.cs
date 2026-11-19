using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketeer.Domain;

namespace Ticketeer.Data.Configurations;

public sealed class TicketQuestionConfiguration : IEntityTypeConfiguration<TicketQuestion>
{
    public void Configure(EntityTypeBuilder<TicketQuestion> builder)
    {
        builder.ToTable("ticket_questions", table => table.HasCheckConstraint("ck_ticket_questions_position_positive", "position > 0"));
        builder.HasKey(x => new { x.TicketId, x.QuestionId });
        builder.Property(x => x.TicketId).HasColumnName("ticket_id");
        builder.Property(x => x.QuestionId).HasColumnName("question_id");
        builder.Property(x => x.Position).HasColumnName("position").IsRequired();
        builder.HasIndex(x => new { x.TicketId, x.Position }).IsUnique();
        builder.HasIndex(x => x.QuestionId);
    }
}
