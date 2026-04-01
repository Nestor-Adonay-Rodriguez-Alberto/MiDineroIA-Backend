using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MiDineroIA_Backend.Application.Interfaces;
using MiDineroIA_Backend.CrossCutting.Auth;
using MiDineroIA_Backend.Domain.Interfaces;

namespace MiDineroIA_Backend.Functions;

/// <summary>
/// Azure Function para el endpoint del dashboard.
/// </summary>
public class DashboardFn
{
    private readonly IDashboardService _dashboardService;
    private readonly ITokenValidator _tokenValidator;
    private readonly ILogger<DashboardFn> _logger;

    public DashboardFn(IDashboardService dashboardService, ITokenValidator tokenValidator, ILogger<DashboardFn> logger)
    {
        _dashboardService = dashboardService;
        _tokenValidator = tokenValidator;
        _logger = logger;
    }



    /// <summary>
    /// GET /api/dashboard?year={year}&month={month}
    /// Obtiene todos los datos del dashboard para el mes especificado.
    /// Si no se envían parámetros, usa el año y mes actuales.
    /// </summary>
    [Function("GetDashboard")]
    public async Task<IActionResult> GetDashboard([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dashboard")] HttpRequest req)
    {
        try
        {
            // Validar JWT y obtener userId
            var userId = AuthHelper.ExtractUserIdFromRequest(req, _tokenValidator);
            if (userId == null)
            {
                return new UnauthorizedObjectResult(new { error = "Token inválido o expirado" });
            }

            // Obtener parámetros de query string
            var now = DateTime.UtcNow;
            int year = now.Year;
            int month = now.Month;

            if (int.TryParse(req.Query["year"], out var parsedYear) && parsedYear > 2000 && parsedYear < 2100)
            {
                year = parsedYear;
            }

            if (int.TryParse(req.Query["month"], out var parsedMonth) && parsedMonth >= 1 && parsedMonth <= 12)
            {
                month = parsedMonth;
            }

            // Obtener datos del dashboard
            var dashboard = await _dashboardService.GetDashboardAsync(userId.Value, year, month);

            return new OkObjectResult(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard: {Message}", ex.Message);
            return new ObjectResult(new { error = "Error al obtener el dashboard", details = ex.Message })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

}
