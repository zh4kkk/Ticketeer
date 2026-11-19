using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketeer.Domain;

namespace Ticketeer.Data.Configurations;

public sealed class LoginEventConfiguration : IEntityTypeConfiguration<LoginEvent>
{
    public void Configure(EntityTypeBuilder<LoginEvent> builder)
    {
        builder.ToTable("login_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
        builder.HasIndex(x => x.CreatedAt);
    }
}
