using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiDineroIA_Backend.Domain.Entities;

namespace MiDineroIA_Backend.Infrastructure.Configurations;

public class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.ToTable("Receipts");

        builder.HasKey(r => r.Id)
            .HasName("PK_Receipts");

        builder.Property(r => r.Id)
            .HasColumnName("id");

        builder.Property(r => r.TransactionId)
            .HasColumnName("transaction_id")
            .IsRequired();

        builder.Property(r => r.ImageUrl)
            .HasColumnName("image_url")
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(r => r.RawOcrText)
            .HasColumnName("raw_ocr_text")
            .HasColumnType("nvarchar(max)");

        builder.Property(r => r.AiExtractedJson)
            .HasColumnName("ai_extracted_json")
            .HasColumnType("nvarchar(max)");

        builder.Property(r => r.ConfidenceScore)
            .HasColumnName("confidence_score")
            .HasPrecision(5, 2);

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        // Foreign keys
        builder.HasOne(r => r.Transaction)
            .WithOne(t => t.Receipt)
            .HasForeignKey<Receipt>(r => r.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
