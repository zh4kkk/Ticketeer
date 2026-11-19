using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketeer.Domain;

namespace Ticketeer.Data.Configurations;

public sealed class AttemptConfiguration : IEntityTypeConfiguration<Attempt>
{
    public void Configure(EntityTypeBuilder<Attempt> builder)
    {
        const string sourceCheck = "(source = 'Random' AND ticket_id IS NULL AND exam_id IS NULL) OR (source = 'Prepared' AND ((kind = 'Training' AND exam_id IS NULL) OR (kind = 'Exam' AND ticket_id IS NULL)))";
        const string kindCheck = "(kind = 'Training' AND is_passed IS NULL) OR (kind = 'Exam')";
        const string statusCheck = "(status <> 'Completed' OR kind <> 'Exam' OR is_passed IS NOT NULL)";
        const string roleCheck = "correct_answers >= 0 AND wrong_answers >= 0 AND question_count > 0 AND (time_limit_seconds IS NULL OR time_limit_seconds > 0) AND (finished_at IS NULL OR finished_at >= started_at)";

        builder.ToTable("attempts", table =>
        {
            table.HasCheckConstraint("ck_attempts_source_references", $"kind IN ('Training', 'Exam') AND {sourceCheck}");
            table.HasCheckConstraint("ck_attempts_status_values", "status IN ('InProgress', 'Completed', 'Failed', 'TimedOut')");
            table.HasCheckConstraint("ck_attempts_kind_values", "kind IN ('Training', 'Exam')");
            table.HasCheckConstraint("ck_attempts_source_values", "source IN ('Prepared', 'Random')");
            table.HasCheckConstraint("ck_attempts_training_passed", kindCheck);
            table.HasCheckConstraint("ck_attempts_completed_passed", statusCheck);
            table.HasCheckConstraint("ck_attempts_counters", roleCheck);
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Source).HasColumnName("source").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.GroupIdSnapshot).HasColumnName("group_id_snapshot");
        builder.Property(x => x.GroupNameSnapshot).HasColumnName("group_name_snapshot").HasMaxLength(200);
        builder.Property(x => x.StartedAt).HasColumnName("started_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.FinishedAt).HasColumnName("finished_at").HasColumnType("timestamptz");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.CorrectAnswers).HasColumnName("correct_answers").IsRequired();
        builder.Property(x => x.WrongAnswers).HasColumnName("wrong_answers").IsRequired();
        builder.Property(x => x.IsPassed).HasColumnName("is_passed");
        builder.Property(x => x.QuestionCount).HasColumnName("question_count").IsRequired();
        builder.Property(x => x.TimeLimitSeconds).HasColumnName("time_limit_seconds");
        builder.Property(x => x.TicketId).HasColumnName("ticket_id");
        builder.Property(x => x.ExamId).HasColumnName("exam_id");
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.StartedAt);
        builder.HasIndex(x => new { x.UserId, x.StartedAt });
    }
}
