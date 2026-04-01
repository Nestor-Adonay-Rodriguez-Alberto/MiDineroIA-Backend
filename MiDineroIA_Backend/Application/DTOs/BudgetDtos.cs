using System.Text.Json.Serialization;

namespace MiDineroIA_Backend.Application.DTOs;

public record BudgetStatusDto(decimal Budget, decimal Spent, decimal Remaining);

public class UpsertBudgetRequestDto
{
    [JsonPropertyName("category_id")]
    public int CategoryId { get; set; }

    [JsonPropertyName("year")]
    public int Year { get; set; }

    [JsonPropertyName("month")]
    public int Month { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
}

public class BudgetResponseDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("category_id")]
    public int CategoryId { get; set; }

    [JsonPropertyName("year")]
    public int Year { get; set; }

    [JsonPropertyName("month")]
    public int Month { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
}
