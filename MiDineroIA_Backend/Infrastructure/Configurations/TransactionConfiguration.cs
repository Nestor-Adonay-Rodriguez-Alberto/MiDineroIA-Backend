using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiDineroIA_Backend.Domain.Entities;

namespace MiDineroIA_Backend.Infrastructure.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.CategoryId)
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.Merchant)
            .HasMaxLength(200);

        builder.Property(t => t.TransactionDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(t => t.Source)
            .HasDefaultValue("TEXT")
            .HasMaxLength(20);

        builder.Property(t => t.IsConfirmed)
            .HasDefaultValue(true);

        builder.Property(t => t.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(t => t.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Foreign keys
        builder.HasOne(t => t.User)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Category)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ChatMessage)
            .WithMany(cm => cm.Transactions)
            .HasForeignKey(t => t.ChatMessageId)
            .OnDelete(DeleteBehavior.SetNull);

        // Navigation properties
        builder.HasOne(t => t.Receipt)
            .WithOne(r => r.Transaction)
            .HasForeignKey<Receipt>(r => r.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
