using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Ticketeer.Models;

namespace Ticketeer.Data.Configurations;

public sealed class AttemptQuestionConfiguration : IEntityTypeConfiguration<AttemptQuestion>
{
    public void Configure(EntityTypeBuilder<AttemptQuestion> builder)
    {
        builder.ToTable("attempt_questions", table =>
        {
            table.HasCheckConstraint("ck_attempt_questions_position_positive", "position > 0");
            table.HasCheckConstraint("ck_attempt_questions_answer_consistency", "(selected_option_position IS NULL AND is_correct IS NULL AND answered_at IS NULL) OR (selected_option_position IS NOT NULL AND selected_option_position > 0 AND is_correct IS NOT NULL AND answered_at IS NOT NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AttemptId).HasColumnName("attempt_id");
        builder.Property(x => x.QuestionId).HasColumnName("question_id");
        builder.Property(x => x.Position).HasColumnName("position");
        builder.Property(x => x.QuestionText).HasColumnName("question_text").HasMaxLength(10000).IsRequired();
        builder.Property(x => x.OptionsSnapshot).HasColumnName("options_snapshot").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.SelectedOptionPosition).HasColumnName("selected_option_position");
        builder.Property(x => x.IsCorrect).HasColumnName("is_correct");
        builder.Property(x => x.AnsweredAt).HasColumnName("answered_at").HasColumnType("timestamptz");
        builder.HasIndex(x => new { x.AttemptId, x.Position }).IsUnique();
        builder.HasIndex(x => x.QuestionId);
        builder.HasOne(x => x.Attempt).WithMany(x => x.AttemptQuestions).HasForeignKey(x => x.AttemptId).OnDelete(DeleteBehavior.Cascade);

        var converter = new ValueConverter<List<AnswerOptionSnapshot>, string>(
            value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
            value => JsonSerializer.Deserialize<List<AnswerOptionSnapshot>>(value, (JsonSerializerOptions?)null) ?? new());
        var comparer = new ValueComparer<List<AnswerOptionSnapshot>>(
            (left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
            value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null).GetHashCode(),
            value => JsonSerializer.Deserialize<List<AnswerOptionSnapshot>>(JsonSerializer.Serialize(value, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null) ?? new());
        builder.Property(x => x.OptionsSnapshot).HasConversion(converter, comparer);
    }
}
