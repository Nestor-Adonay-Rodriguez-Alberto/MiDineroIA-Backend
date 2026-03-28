namespace MiDineroIA_Backend.Domain.Entities;

public class Transaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public int? ChatMessageId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? Merchant { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Source { get; set; } = "TEXT"; // 'TEXT', 'IMAGE', 'MANUAL'
    public bool IsConfirmed { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ChatMessage? ChatMessage { get; set; }
    public Receipt? Receipt { get; set; }
}
