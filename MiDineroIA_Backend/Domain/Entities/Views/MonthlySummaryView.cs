using System.ComponentModel.DataAnnotations.Schema;

namespace MiDineroIA_Backend.Domain.Entities.Views;

/// <summary>
/// Mapea la vista vw_MonthlySummary para obtener totales mensuales.
/// La vista tiene columnas: user_id, year, month, transaction_type, group_name, total_real
/// </summary>
public class MonthlySummaryView
{
    [Column("user_id")]
    public int UserId { get; set; }
    
    [Column("year")]
    public int Year { get; set; }
    
    [Column("month")]
    public int Month { get; set; }
    
    [Column("transaction_type")]
    public string TransactionType { get; set; } = string.Empty;
    
    [Column("group_name")]
    public string GroupName { get; set; } = string.Empty;
    
    [Column("total_real")]
    public decimal TotalReal { get; set; }
}
