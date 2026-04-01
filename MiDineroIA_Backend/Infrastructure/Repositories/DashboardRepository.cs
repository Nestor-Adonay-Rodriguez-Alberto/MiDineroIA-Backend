using Microsoft.EntityFrameworkCore;
using MiDineroIA_Backend.Domain.Entities.Views;
using MiDineroIA_Backend.Domain.Interfaces;
using MiDineroIA_Backend.Infrastructure.Database;

namespace MiDineroIA_Backend.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly AppDbContext _context;

    public DashboardRepository(AppDbContext context)
    {
        _context = context;
    }



    // CADA METODO OBTIENE LOS DATOS NECESARIOS PARA EL DASHBOARD, FILTRANDO POR USUARIO, AÑO Y MES
    public async Task<List<MonthlySummaryView>> GetMonthlySummariesAsync(int userId, int year, int month)
    {
        return await _context.MonthlySummaries
            .Where(s => s.UserId == userId && s.Year == year && s.Month == month)
            .ToListAsync();
    }


    public async Task<List<CategoryDetailView>> GetCategoryDetailsAsync(int userId, int year, int month)
    {
        return await _context.CategoryDetails
            .Where(c => c.UserId == userId && c.Year == year && c.Month == month)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();
    }


    public async Task<List<ExpenseDistributionView>> GetExpenseDistributionsAsync(int userId, int year, int month)
    {
        return await _context.ExpenseDistributions
            .Where(e => e.UserId == userId && e.Year == year && e.Month == month)
            .ToListAsync();
    }

}
