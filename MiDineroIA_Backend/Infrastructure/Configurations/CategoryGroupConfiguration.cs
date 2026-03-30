using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiDineroIA_Backend.Domain.Entities;

namespace MiDineroIA_Backend.Infrastructure.Configurations;

public class CategoryGroupConfiguration : IEntityTypeConfiguration<CategoryGroup>
{
    public void Configure(EntityTypeBuilder<CategoryGroup> builder)
    {
        builder.ToTable("CategoryGroups");

        builder.HasKey(cg => cg.Id)
            .HasName("PK_CategoryGroups");

        builder.Property(cg => cg.Id)
            .HasColumnName("id");

        builder.Property(cg => cg.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(cg => cg.TransactionType)
            .HasColumnName("transaction_type")
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(cg => cg.DisplayOrder)
            .HasColumnName("display_order")
            .HasDefaultValue(0);

        builder.Property(cg => cg.Icon)
            .HasColumnName("icon")
            .HasMaxLength(50);

        // Navigation properties
        builder.HasMany(cg => cg.Categories)
            .WithOne(c => c.CategoryGroup)
            .HasForeignKey(c => c.CategoryGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
