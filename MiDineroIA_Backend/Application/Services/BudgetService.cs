using MiDineroIA_Backend.Application.DTOs;
using MiDineroIA_Backend.Application.Interfaces;
using MiDineroIA_Backend.Domain.Entities;
using MiDineroIA_Backend.Domain.Interfaces;

namespace MiDineroIA_Backend.Application.Services;

public class BudgetService : IBudgetService
{
    private readonly IBudgetRepository _budgetRepository;

    public BudgetService(IBudgetRepository budgetRepository)
    {
        _budgetRepository = budgetRepository;
    }

    public async Task<BudgetResponseDto> UpsertAsync(int userId, UpsertBudgetRequestDto dto)
    {
        var budget = new MonthlyBudget
        {
            UserId = userId,
            CategoryId = dto.CategoryId,
            Year = dto.Year,
            Month = dto.Month,
            Amount = dto.Amount
        };

        var saved = await _budgetRepository.UpsertAsync(budget);

        return new BudgetResponseDto
        {
            Id = saved.Id,
            CategoryId = saved.CategoryId,
            Year = saved.Year,
            Month = saved.Month,
            Amount = saved.Amount
        };
    }
}
