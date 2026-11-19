using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketeer.Domain;

namespace Ticketeer.Data.Configurations;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(4000);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.HasMany(x => x.TicketQuestions).WithOne(x => x.Ticket).HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Attempts).WithOne(x => x.Ticket).HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.SetNull);
    }
}
