using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiDineroIA_Backend.Application.DTOs;

/// <summary>
/// Respuesta principal de Claude API después de procesar un mensaje.
/// </summary>
public record ClaudeResponseDto
{
    [JsonPropertyName("intent")]
    public string Intent { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public JsonElement Data { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("needs_confirmation")]
    public bool NeedsConfirmation { get; init; }

    /// <summary>
    /// Deserializa Data como ClaudeTransactionData (para intent REGISTER_TRANSACTION).
    /// </summary>
    public ClaudeTransactionData? GetTransactionData()
    {
        if (Data.ValueKind == JsonValueKind.Undefined || Data.ValueKind == JsonValueKind.Null)
            return null;
        return JsonSerializer.Deserialize<ClaudeTransactionData>(Data.GetRawText());
    }

    /// <summary>
    /// Deserializa Data como ClaudeBudgetData (para intent SET_BUDGET).
    /// </summary>
    public ClaudeBudgetData? GetBudgetData()
    {
        if (Data.ValueKind == JsonValueKind.Undefined || Data.ValueKind == JsonValueKind.Null)
            return null;
        return JsonSerializer.Deserialize<ClaudeBudgetData>(Data.GetRawText());
    }

    /// <summary>
    /// Deserializa Data como ClaudeQueryData (para intent GENERAL_QUERY).
    /// </summary>
    public ClaudeQueryData? GetQueryData()
    {
        if (Data.ValueKind == JsonValueKind.Undefined || Data.ValueKind == JsonValueKind.Null)
            return null;
        return JsonSerializer.Deserialize<ClaudeQueryData>(Data.GetRawText());
    }
}

/// <summary>
/// Datos de transacción extraídos por Claude (intent: REGISTER_TRANSACTION).
/// </summary>
public record ClaudeTransactionData
{
    [JsonPropertyName("transaction_type")]
    public string TransactionType { get; init; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("category_name")]
    public string CategoryName { get; init; } = string.Empty;

    [JsonPropertyName("category_id")]
    public int CategoryId { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("merchant")]
    public string? Merchant { get; init; }

    [JsonPropertyName("transaction_date")]
    public string TransactionDate { get; init; } = string.Empty;

    [JsonPropertyName("confidence_score")]
    public int ConfidenceScore { get; init; }

    /// <summary>
    /// Artículos extraídos de OCR (opcional, solo para facturas).
    /// </summary>
    [JsonPropertyName("ocr_items")]
    public List<ClaudeOcrItem>? OcrItems { get; init; }
}

/// <summary>
/// Artículo individual extraído del texto OCR de una factura.
/// </summary>
public record ClaudeOcrItem
{
    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; init; }

    [JsonPropertyName("unit_price")]
    public decimal UnitPrice { get; init; }

    [JsonPropertyName("total")]
    public decimal Total { get; init; }
}

/// <summary>
/// Datos de presupuesto extraídos por Claude (intent: SET_BUDGET).
/// </summary>
public record ClaudeBudgetData
{
    [JsonPropertyName("budgets")]
    public List<ClaudeBudgetItem> Budgets { get; init; } = new();
}

/// <summary>
/// Un presupuesto individual para una categoría.
/// </summary>
public record ClaudeBudgetItem
{
    [JsonPropertyName("category_name")]
    public string CategoryName { get; init; } = string.Empty;

    [JsonPropertyName("category_id")]
    public int CategoryId { get; init; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("year")]
    public int Year { get; init; }

    [JsonPropertyName("month")]
    public int Month { get; init; }
}

/// <summary>
/// Datos de consulta general (intent: GENERAL_QUERY).
/// </summary>
public record ClaudeQueryData
{
    [JsonPropertyName("query_type")]
    public string QueryType { get; init; } = string.Empty;

    [JsonPropertyName("category_id")]
    public int? CategoryId { get; init; }

    [JsonPropertyName("category_name")]
    public string? CategoryName { get; init; }
}

/// <summary>
/// Categoría alternativa sugerida por Claude cuando no está seguro de la clasificación.
/// </summary>
public record SuggestedAlternative
{
    [JsonPropertyName("category_name")]
    public string CategoryName { get; init; } = string.Empty;

    [JsonPropertyName("category_id")]
    public int CategoryId { get; init; }
}
