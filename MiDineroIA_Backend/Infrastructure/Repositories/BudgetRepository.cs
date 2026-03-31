using Microsoft.EntityFrameworkCore;
using MiDineroIA_Backend.Application.DTOs;
using MiDineroIA_Backend.Domain.Entities;
using MiDineroIA_Backend.Domain.Interfaces;
using MiDineroIA_Backend.Infrastructure.Database;

namespace MiDineroIA_Backend.Infrastructure.Repositories;

public class BudgetRepository : IBudgetRepository
{
    private readonly AppDbContext _context;

    public BudgetRepository(AppDbContext context)
    {
        _context = context;
    }




    // CREA O ACTUALIZA UN PRESUPUESTO MENSUAL
    public async Task<MonthlyBudget> UpsertAsync(MonthlyBudget budget)
    {
        var existing = await _context.MonthlyBudgets
            .FirstOrDefaultAsync(b => 
                b.UserId == budget.UserId &&
                b.CategoryId == budget.CategoryId &&
                b.Year == budget.Year &&
                b.Month == budget.Month);

        if (existing != null)
        {
            existing.Amount = budget.Amount;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return existing;
        }
        else
        {
            budget.CreatedAt = DateTime.UtcNow;
            budget.UpdatedAt = DateTime.UtcNow;
            _context.MonthlyBudgets.Add(budget);
            await _context.SaveChangesAsync();
            return budget;
        }
    }


    // OBTENER EL ESTADO DEL PRESUPUESTO PARA UN USUARIO, CATEGORÍA Y PERÍODO ESPECÍFICO
    public async Task<BudgetStatusDto> GetBudgetStatusAsync(int userId, int categoryId, int year, int month)
    {
        var budget = await _context.MonthlyBudgets
            .Where(b => 
                b.UserId == userId &&
                b.CategoryId == categoryId &&
                b.Year == year &&
                b.Month == month)
            .Select(b => b.Amount)
            .FirstOrDefaultAsync();

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        var spent = await _context.Transactions
            .Where(t => 
                t.UserId == userId &&
                t.CategoryId == categoryId &&
                t.IsConfirmed &&
                t.TransactionDate >= startDate &&
                t.TransactionDate < endDate)
            .SumAsync(t => t.Amount);

        return new BudgetStatusDto(Budget: budget, Spent: spent,Remaining: budget - spent);
    }

}
