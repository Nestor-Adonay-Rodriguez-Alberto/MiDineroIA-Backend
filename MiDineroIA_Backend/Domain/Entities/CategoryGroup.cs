namespace MiDineroIA_Backend.Domain.Entities;

public class CategoryGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty; // 'INGRESO' o 'EGRESO'
    public int DisplayOrder { get; set; }
    public string? Icon { get; set; }

    // Navigation properties
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
