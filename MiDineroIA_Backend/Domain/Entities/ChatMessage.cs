namespace MiDineroIA_Backend.Domain.Entities;

public class ChatMessage
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string MessageType { get; set; } = string.Empty; // 'USER_TEXT', 'USER_IMAGE', 'AI_RESPONSE'
    public string? Content { get; set; }
    public string? ImageUrl { get; set; }
    public bool AiProcessed { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
