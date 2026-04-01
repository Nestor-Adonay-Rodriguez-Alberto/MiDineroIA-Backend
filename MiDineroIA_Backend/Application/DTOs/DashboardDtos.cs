using System.Text.Json.Serialization;

namespace MiDineroIA_Backend.Application.DTOs;

/// <summary>
/// Respuesta completa del dashboard.
/// </summary>
public class DashboardResponseDto
{
    [JsonPropertyName("summary")]
    public SummaryDto Summary { get; set; } = new();

    [JsonPropertyName("income_detail")]
    public List<CategoryBudgetRealDto> IncomeDetail { get; set; } = new();

    [JsonPropertyName("expense_groups")]
    public List<ExpenseGroupDto> ExpenseGroups { get; set; } = new();

    [JsonPropertyName("expense_distribution")]
    public List<ExpenseDistributionDto> ExpenseDistribution { get; set; } = new();
}

/// <summary>
/// Resumen de totales del mes.
/// </summary>
public class SummaryDto
{
    [JsonPropertyName("total_income")]
    public decimal TotalIncome { get; set; }

    [JsonPropertyName("total_expenses")]
    public decimal TotalExpenses { get; set; }

    [JsonPropertyName("balance")]
    public decimal Balance { get; set; }
}

/// <summary>
/// Detalle de presupuesto vs real por categoría.
/// </summary>
public class CategoryBudgetRealDto
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("budget")]
    public decimal Budget { get; set; }

    [JsonPropertyName("real")]
    public decimal Real { get; set; }
}

/// <summary>
/// Grupo de gastos con sus categorías.
/// </summary>
public class ExpenseGroupDto
{
    [JsonPropertyName("group_name")]
    public string GroupName { get; set; } = string.Empty;

    [JsonPropertyName("categories")]
    public List<CategoryBudgetRealDto> Categories { get; set; } = new();
}

/// <summary>
/// Distribución de gastos para el gráfico de dona.
/// </summary>
public class ExpenseDistributionDto
{
    [JsonPropertyName("group")]
    public string Group { get; set; } = string.Empty;

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("percentage")]
    public decimal Percentage { get; set; }
}
