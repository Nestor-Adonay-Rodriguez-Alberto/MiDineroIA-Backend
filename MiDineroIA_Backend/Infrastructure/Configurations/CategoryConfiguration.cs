using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiDineroIA_Backend.Domain.Entities;

namespace MiDineroIA_Backend.Infrastructure.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CategoryGroupId)
            .IsRequired();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.IsDefault)
            .HasDefaultValue(true);

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        builder.Property(c => c.DisplayOrder)
            .HasDefaultValue(0);

        // Foreign keys
        builder.HasOne(c => c.CategoryGroup)
            .WithMany(cg => cg.Categories)
            .HasForeignKey(c => c.CategoryGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.User)
            .WithMany(u => u.Categories)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Navigation properties
        builder.HasMany(c => c.MonthlyBudgets)
            .WithOne(mb => mb.Category)
            .HasForeignKey(mb => mb.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Transactions)
            .WithOne(t => t.Category)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
