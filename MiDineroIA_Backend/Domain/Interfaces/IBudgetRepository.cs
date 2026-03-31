using MiDineroIA_Backend.Application.DTOs;
using MiDineroIA_Backend.Domain.Entities;

namespace MiDineroIA_Backend.Domain.Interfaces;

/// <summary>
/// Repositorio para operaciones de presupuestos mensuales.
/// </summary>
public interface IBudgetRepository
{
    /// <summary>
    /// Crea o actualiza un presupuesto mensual.
    /// La combinación (UserId, CategoryId, Year, Month) determina si es INSERT o UPDATE.
    /// </summary>
    /// <param name="budget">Presupuesto a guardar.</param>
    /// <returns>Presupuesto guardado con Id asignado.</returns>
    Task<MonthlyBudget> UpsertAsync(MonthlyBudget budget);

    /// <summary>
    /// Obtiene el estado del presupuesto de una categoría para un mes específico.
    /// Calcula el presupuesto definido y el monto gastado en transacciones.
    /// </summary>
    /// <param name="userId">Id del usuario.</param>
    /// <param name="categoryId">Id de la categoría.</param>
    /// <param name="year">Año.</param>
    /// <param name="month">Mes (1-12).</param>
    /// <returns>DTO con Budget, Spent y Remaining. Budget es 0 si no hay presupuesto definido.</returns>
    Task<BudgetStatusDto> GetBudgetStatusAsync(int userId, int categoryId, int year, int month);
}
