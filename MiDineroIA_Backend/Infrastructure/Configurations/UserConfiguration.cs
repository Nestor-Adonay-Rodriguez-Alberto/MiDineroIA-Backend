using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiDineroIA_Backend.Domain.Entities;

namespace MiDineroIA_Backend.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id)
            .HasName("PK_Users");

        builder.Property(u => u.Id)
            .HasColumnName("id");

        builder.Property(u => u.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(u => u.MonthlyIncome)
            .HasColumnName("monthly_income")
            .HasPrecision(12, 2);

        builder.Property(u => u.Currency)
            .HasColumnName("currency")
            .HasDefaultValue("USD")
            .HasMaxLength(10);

        builder.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("GETUTCDATE()");

        // Navigation properties
        builder.HasMany(u => u.Categories)
            .WithOne(c => c.User)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.MonthlyBudgets)
            .WithOne(mb => mb.User)
            .HasForeignKey(mb => mb.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.ChatMessages)
            .WithOne(cm => cm.User)
            .HasForeignKey(cm => cm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Transactions)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
