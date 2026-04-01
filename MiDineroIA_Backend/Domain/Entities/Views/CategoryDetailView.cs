using System.ComponentModel.DataAnnotations.Schema;

namespace MiDineroIA_Backend.Domain.Entities.Views;

/// <summary>
/// Mapea la vista vw_CategoryDetail para obtener presupuesto vs real por categoría.
/// Columnas: user_id, year, month, group_name, transaction_type, category_name, display_order, budget_amount, real_amount
/// </summary>
public class CategoryDetailView
{
    [Column("user_id")]
    public int UserId { get; set; }
    
    [Column("year")]
    public int Year { get; set; }
    
    [Column("month")]
    public int Month { get; set; }
    
    [Column("group_name")]
    public string GroupName { get; set; } = string.Empty;
    
    [Column("transaction_type")]
    public string TransactionType { get; set; } = string.Empty;
    
    [Column("category_name")]
    public string CategoryName { get; set; } = string.Empty;
    
    [Column("display_order")]
    public int DisplayOrder { get; set; }
    
    [Column("budget_amount")]
    public decimal BudgetAmount { get; set; }
    
    [Column("real_amount")]
    public decimal RealAmount { get; set; }
}
