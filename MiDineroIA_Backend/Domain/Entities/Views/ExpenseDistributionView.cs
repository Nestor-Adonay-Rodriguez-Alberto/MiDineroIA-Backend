using System.ComponentModel.DataAnnotations.Schema;

namespace MiDineroIA_Backend.Domain.Entities.Views;

/// <summary>
/// Mapea la vista vw_ExpenseDistribution para el gráfico de dona de gastos.
/// Columnas: user_id, year, month, group_name, total_amount, percentage
/// </summary>
public class ExpenseDistributionView
{
    [Column("user_id")]
    public int UserId { get; set; }
    
    [Column("year")]
    public int Year { get; set; }
    
    [Column("month")]
    public int Month { get; set; }
    
    [Column("group_name")]
    public string GroupName { get; set; } = string.Empty;
    
    [Column("total_amount")]
    public decimal TotalAmount { get; set; }
    
    [Column("percentage")]
    public decimal Percentage { get; set; }
}
