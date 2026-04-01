using System.Text.Json.Serialization;

namespace MiDineroIA_Backend.Application.DTOs;

/// <summary>
/// Request para actualizar una transacción.
/// </summary>
public record UpdateTransactionRequestDto(
    [property: JsonPropertyName("category_id")] int CategoryId,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("description")] string? Description
);

/// <summary>
/// Respuesta después de una operación sobre transacción.
/// </summary>
public record TransactionResponseDto
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("category_id")]
    public int CategoryId { get; init; }

    [JsonPropertyName("category_name")]
    public string? CategoryName { get; init; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("merchant")]
    public string? Merchant { get; init; }

    [JsonPropertyName("transaction_date")]
    public DateTime TransactionDate { get; init; }

    [JsonPropertyName("is_confirmed")]
    public bool IsConfirmed { get; init; }
}
