using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiDineroIA_Backend.Domain.Entities;

namespace MiDineroIA_Backend.Infrastructure.Configurations;

public class MonthlyBudgetConfiguration : IEntityTypeConfiguration<MonthlyBudget>
{
    public void Configure(EntityTypeBuilder<MonthlyBudget> builder)
    {
        builder.ToTable("MonthlyBudgets");

        builder.HasKey(mb => mb.Id)
            .HasName("PK_MonthlyBudgets");

        builder.Property(mb => mb.Id)
            .HasColumnName("id");

        builder.Property(mb => mb.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(mb => mb.CategoryId)
            .HasColumnName("category_id")
            .IsRequired();

        builder.Property(mb => mb.Year)
            .HasColumnName("year")
            .IsRequired();

        builder.Property(mb => mb.Month)
            .HasColumnName("month")
            .IsRequired();

        builder.Property(mb => mb.Amount)
            .HasColumnName("amount")
            .HasPrecision(12, 2)
            .HasDefaultValue(0);

        builder.Property(mb => mb.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(mb => mb.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("GETUTCDATE()");

        // Unique constraint
        builder.HasIndex(mb => new { mb.UserId, mb.CategoryId, mb.Year, mb.Month })
            .IsUnique();

        // Foreign keys
        builder.HasOne(mb => mb.User)
            .WithMany(u => u.MonthlyBudgets)
            .HasForeignKey(mb => mb.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mb => mb.Category)
            .WithMany(c => c.MonthlyBudgets)
            .HasForeignKey(mb => mb.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
