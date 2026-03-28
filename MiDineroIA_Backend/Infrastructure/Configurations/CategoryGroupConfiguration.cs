using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiDineroIA_Backend.Domain.Entities;

namespace MiDineroIA_Backend.Infrastructure.Configurations;

public class CategoryGroupConfiguration : IEntityTypeConfiguration<CategoryGroup>
{
    public void Configure(EntityTypeBuilder<CategoryGroup> builder)
    {
        builder.ToTable("CategoryGroups");

        builder.HasKey(cg => cg.Id);

        builder.Property(cg => cg.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(cg => cg.TransactionType)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(cg => cg.DisplayOrder)
            .HasDefaultValue(0);

        builder.Property(cg => cg.Icon)
            .HasMaxLength(50);

        // Navigation properties
        builder.HasMany(cg => cg.Categories)
            .WithOne(c => c.CategoryGroup)
            .HasForeignKey(c => c.CategoryGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
