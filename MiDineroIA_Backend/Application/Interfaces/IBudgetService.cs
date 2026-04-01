using MiDineroIA_Backend.Application.DTOs;

namespace MiDineroIA_Backend.Application.Interfaces;

public interface IBudgetService
{
    Task<BudgetResponseDto> UpsertAsync(int userId, UpsertBudgetRequestDto dto);
}
