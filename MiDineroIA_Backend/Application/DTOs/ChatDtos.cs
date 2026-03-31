using System.Text.Json.Serialization;

namespace MiDineroIA_Backend.Application.DTOs;

/// <summary>
/// Request para enviar un mensaje al chat.
/// </summary>
public record ChatRequestDto(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("image_base64")] string? ImageBase64
);

/// <summary>
/// Respuesta principal del ChatService después de procesar un mensaje.
/// </summary>
public record ChatResponseDto
{
    /// <summary>
    /// Intención detectada: REGISTER_TRANSACTION, SET_BUDGET, o GENERAL_QUERY.
    /// </summary>
    [JsonPropertyName("intent")]
    public string Intent { get; init; } = string.Empty;

    /// <summary>
    /// Id de la transacción creada (solo para REGISTER_TRANSACTION).
    /// </summary>
    [JsonPropertyName("transaction_id")]
    public int? TransactionId { get; init; }

    /// <summary>
    /// Mensaje para mostrar al usuario.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Datos específicos según el intent.
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; init; }

    /// <summary>
    /// Indica si la acción requiere confirmación del usuario.
    /// </summary>
    [JsonPropertyName("needs_confirmation")]
    public bool NeedsConfirmation { get; init; }

    /// <summary>
    /// Información del presupuesto de la categoría (solo para REGISTER_TRANSACTION si existe presupuesto).
    /// </summary>
    [JsonPropertyName("budget_info")]
    public BudgetInfoDto? BudgetInfo { get; init; }

    /// <summary>
    /// Categorías alternativas sugeridas cuando Claude no está seguro de la clasificación.
    /// </summary>
    [JsonPropertyName("suggested_alternatives")]
    public List<SuggestedAlternativeDto>? SuggestedAlternatives { get; init; }
}

/// <summary>
/// Información del estado del presupuesto para una categoría.
/// </summary>
public record BudgetInfoDto(
    [property: JsonPropertyName("budget")] decimal Budget,
    [property: JsonPropertyName("spent")] decimal Spent,
    [property: JsonPropertyName("remaining")] decimal Remaining
);

/// <summary>
/// Categoría alternativa sugerida por Claude.
/// </summary>
public record SuggestedAlternativeDto(
    [property: JsonPropertyName("category_name")] string CategoryName,
    [property: JsonPropertyName("category_id")] int CategoryId
);

/// <summary>
/// Datos de transacción para incluir en la respuesta.
/// </summary>
public record TransactionDataDto
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
}

/// <summary>
/// Datos de presupuestos configurados para incluir en la respuesta.
/// </summary>
public record BudgetDataDto(
    [property: JsonPropertyName("budgets")] List<BudgetItemDto> Budgets
);

/// <summary>
/// Un presupuesto individual configurado.
/// </summary>
public record BudgetItemDto(
    [property: JsonPropertyName("category_name")] string CategoryName,
    [property: JsonPropertyName("category_id")] int CategoryId,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("year")] int Year,
    [property: JsonPropertyName("month")] int Month
);

/// <summary>
/// Mensaje de chat para el historial.
/// </summary>
public record ChatMessageDto
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("message_type")]
    public string MessageType { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; init; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Datos de consulta general para incluir en la respuesta.
/// </summary>
public record QueryDataDto(
    [property: JsonPropertyName("query_type")] string QueryType
);
