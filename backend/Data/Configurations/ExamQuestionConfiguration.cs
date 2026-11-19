using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketeer.Domain;

namespace Ticketeer.Data.Configurations;

public sealed class ExamQuestionConfiguration : IEntityTypeConfiguration<ExamQuestion>
{
    public void Configure(EntityTypeBuilder<ExamQuestion> builder)
    {
        builder.ToTable("exam_questions", table => table.HasCheckConstraint("ck_exam_questions_position_positive", "position > 0"));
        builder.HasKey(x => new { x.ExamId, x.QuestionId });
        builder.Property(x => x.ExamId).HasColumnName("exam_id");
        builder.Property(x => x.QuestionId).HasColumnName("question_id");
        builder.Property(x => x.Position).HasColumnName("position").IsRequired();
        builder.HasIndex(x => new { x.ExamId, x.Position }).IsUnique();
        builder.HasIndex(x => x.QuestionId);
    }
}
