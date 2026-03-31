namespace MiDineroIA_Backend.Application.DTOs;

/// <summary>
/// DTO que representa el estado del presupuesto de una categoría para un mes específico.
/// </summary>
/// <param name="Budget">Monto presupuestado para la categoría.</param>
/// <param name="Spent">Monto gastado hasta el momento en esa categoría.</param>
/// <param name="Remaining">Diferencia entre presupuesto y gastado (puede ser negativo si se excedió).</param>
public record BudgetStatusDto(decimal Budget, decimal Spent, decimal Remaining);
