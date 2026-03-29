using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiDineroIA_Backend.Domain.Entities;

namespace MiDineroIA_Backend.Infrastructure.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id)
            .HasName("PK_Categories");

        builder.Property(c => c.Id)
            .HasColumnName("id");

        builder.Property(c => c.CategoryGroupId)
            .HasColumnName("category_group_id")
            .IsRequired();

        builder.Property(c => c.UserId)
            .HasColumnName("user_id");

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.IsDefault)
            .HasColumnName("is_default")
            .HasDefaultValue(true);

        builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(c => c.DisplayOrder)
            .HasColumnName("display_order")
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
