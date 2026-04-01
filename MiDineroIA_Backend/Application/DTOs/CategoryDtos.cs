using System.Text.Json.Serialization;

namespace MiDineroIA_Backend.Application.DTOs;

public class CategoryDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class CategoryGroupDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("transaction_type")]
    public string TransactionType { get; set; } = string.Empty;

    [JsonPropertyName("categories")]
    public List<CategoryDto> Categories { get; set; } = new();
}

public class CreateCategoryRequestDto
{
    [JsonPropertyName("category_group_id")]
    public int CategoryGroupId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
