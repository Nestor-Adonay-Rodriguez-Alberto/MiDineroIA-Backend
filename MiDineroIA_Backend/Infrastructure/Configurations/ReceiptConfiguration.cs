using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiDineroIA_Backend.Domain.Entities;

namespace MiDineroIA_Backend.Infrastructure.Configurations;

public class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.ToTable("Receipts");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TransactionId)
            .IsRequired();

        builder.Property(r => r.ImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(r => r.RawOcrText)
            .HasColumnType("nvarchar(max)");

        builder.Property(r => r.AiExtractedJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(r => r.ConfidenceScore)
            .HasPrecision(5, 2);

        builder.Property(r => r.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Foreign keys
        builder.HasOne(r => r.Transaction)
            .WithOne(t => t.Receipt)
            .HasForeignKey<Receipt>(r => r.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
