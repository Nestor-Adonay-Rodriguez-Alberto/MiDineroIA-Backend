using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiDineroIA_Backend.Domain.Entities;

namespace MiDineroIA_Backend.Infrastructure.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("ChatMessages");

        builder.HasKey(cm => cm.Id);

        builder.Property(cm => cm.UserId)
            .IsRequired();

        builder.Property(cm => cm.MessageType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(cm => cm.Content)
            .HasColumnType("nvarchar(max)");

        builder.Property(cm => cm.ImageUrl)
            .HasMaxLength(500);

        builder.Property(cm => cm.AiProcessed)
            .HasDefaultValue(false);

        builder.Property(cm => cm.CreatedAt)
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
