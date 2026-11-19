using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketeer.Domain;

namespace Ticketeer.Data.Configurations;

public sealed class AnswerOptionConfiguration : IEntityTypeConfiguration<AnswerOption>
{
    public void Configure(EntityTypeBuilder<AnswerOption> builder)
    {
        builder.ToTable("answer_options", table => table.HasCheckConstraint("ck_answer_options_position_positive", "position > 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.QuestionId).HasColumnName("question_id");
        builder.Property(x => x.Text).HasColumnName("text").HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Position).HasColumnName("position").IsRequired();
        builder.Property(x => x.IsCorrect).HasColumnName("is_correct").IsRequired();
        builder.HasIndex(x => new { x.QuestionId, x.Position }).IsUnique();
        builder.HasIndex(x => x.QuestionId).HasFilter("is_correct = true").IsUnique();
    }
}
