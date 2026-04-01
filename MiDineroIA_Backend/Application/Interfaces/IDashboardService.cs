using MiDineroIA_Backend.Application.DTOs;

namespace MiDineroIA_Backend.Application.Interfaces;

/// <summary>
/// Servicio para obtener datos del dashboard.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Obtiene todos los datos del dashboard para un mes específico.
    /// </summary>
    /// <param name="userId">Id del usuario.</param>
    /// <param name="year">Año.</param>
    /// <param name="month">Mes (1-12).</param>
    /// <returns>Datos completos del dashboard.</returns>
    Task<DashboardResponseDto> GetDashboardAsync(int userId, int year, int month);
}
