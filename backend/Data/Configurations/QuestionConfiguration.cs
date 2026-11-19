using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketeer.Models;

namespace Ticketeer.Data.Configurations;

public sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("questions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TopicId).HasColumnName("topic_id");
        builder.Property(x => x.Text).HasColumnName("text").HasMaxLength(10000).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.HasIndex(x => x.TopicId);
        builder.HasMany(x => x.AnswerOptions).WithOne(x => x.Question).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.TicketQuestions).WithOne(x => x.Question).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.ExamQuestions).WithOne(x => x.Question).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.AttemptQuestions).WithOne(x => x.Question).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.SetNull);
    }
}
