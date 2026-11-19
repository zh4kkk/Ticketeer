using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketeer.Domain;

namespace Ticketeer.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", table => table.HasCheckConstraint("ck_users_role_group", "(role = 'Admin' AND group_id IS NULL) OR role = 'Cashier'"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Login).HasColumnName("login").HasMaxLength(100).IsRequired();
        builder.Property(x => x.NormalizedLogin).HasColumnName("normalized_login").HasMaxLength(100).IsRequired();
        builder.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(500).IsRequired();
        builder.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.GroupId).HasColumnName("group_id");
        builder.HasIndex(x => x.NormalizedLogin).IsUnique();
        builder.HasIndex(x => x.GroupId);
        builder.HasMany(x => x.LoginEvents).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Attempts).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
