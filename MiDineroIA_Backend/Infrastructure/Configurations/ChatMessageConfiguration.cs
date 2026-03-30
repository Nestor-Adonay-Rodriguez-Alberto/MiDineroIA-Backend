using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiDineroIA_Backend.Domain.Entities;

namespace MiDineroIA_Backend.Infrastructure.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("ChatMessages");

        builder.HasKey(cm => cm.Id)
            .HasName("PK_ChatMessages");

        builder.Property(cm => cm.Id)
            .HasColumnName("id");

        builder.Property(cm => cm.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(cm => cm.MessageType)
            .HasColumnName("message_type")
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(cm => cm.Content)
            .HasColumnName("content")
            .HasColumnType("nvarchar(max)");

        builder.Property(cm => cm.ImageUrl)
            .HasColumnName("image_url")
            .HasMaxLength(500);

        builder.Property(cm => cm.AiProcessed)
            .HasColumnName("ai_processed")
            .HasDefaultValue(false);

        builder.Property(cm => cm.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        // Foreign keys
        builder.HasOne(cm => cm.User)
            .WithMany(u => u.ChatMessages)
            .HasForeignKey(cm => cm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation properties
        builder.HasMany(cm => cm.Transactions)
            .WithOne(t => t.ChatMessage)
            .HasForeignKey(t => t.ChatMessageId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
