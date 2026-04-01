using MiDineroIA_Backend.Domain.Entities.Views;

namespace MiDineroIA_Backend.Domain.Interfaces;

public interface IDashboardRepository
{
    Task<List<MonthlySummaryView>> GetMonthlySummariesAsync(int userId, int year, int month);
    Task<List<CategoryDetailView>> GetCategoryDetailsAsync(int userId, int year, int month);
    Task<List<ExpenseDistributionView>> GetExpenseDistributionsAsync(int userId, int year, int month);
}
