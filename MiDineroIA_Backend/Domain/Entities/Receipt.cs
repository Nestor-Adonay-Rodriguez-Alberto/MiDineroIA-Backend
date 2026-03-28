namespace MiDineroIA_Backend.Domain.Entities;

public class Receipt
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? RawOcrText { get; set; }
    public string? AiExtractedJson { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Transaction Transaction { get; set; } = null!;
}
