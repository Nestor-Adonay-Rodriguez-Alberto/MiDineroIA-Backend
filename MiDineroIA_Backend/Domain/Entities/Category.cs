namespace MiDineroIA_Backend.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public int CategoryGroupId { get; set; }
    public int? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    // Navigation properties
    public CategoryGroup CategoryGroup { get; set; } = null!;
    public User? User { get; set; }
    public ICollection<MonthlyBudget> MonthlyBudgets { get; set; } = new List<MonthlyBudget>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
