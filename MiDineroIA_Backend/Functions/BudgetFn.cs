using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MiDineroIA_Backend.Application.DTOs;
using MiDineroIA_Backend.CrossCutting.Auth;
using MiDineroIA_Backend.Domain.Entities;
using MiDineroIA_Backend.Domain.Interfaces;

namespace MiDineroIA_Backend.Functions;

public class BudgetFn
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly ITokenValidator _tokenValidator;
    private readonly ILogger<BudgetFn> _logger;

    public BudgetFn(
        IBudgetRepository budgetRepository,
        ITokenValidator tokenValidator,
        ILogger<BudgetFn> logger)
    {
        _budgetRepository = budgetRepository;
        _tokenValidator = tokenValidator;
        _logger = logger;
    }

    [Function("BudgetUpsert")]
    public async Task<IActionResult> Upsert(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "budgets")] HttpRequest req)
    {
        try
        {
            var userId = AuthHelper.ExtractUserIdFromRequest(req, _tokenValidator);
            if (userId is null)
            {
                return new UnauthorizedObjectResult(new { error = "Token inválido o expirado" });
            }

            var request = await req.ReadFromJsonAsync<UpsertBudgetRequestDto>();
            if (request is null)
            {
                return new BadRequestObjectResult(new { error = "Request body inválido" });
            }

            if (request.Amount < 0)
            {
                return new BadRequestObjectResult(new { error = "El monto no puede ser negativo" });
            }

            if (request.Month < 1 || request.Month > 12)
            {
                return new BadRequestObjectResult(new { error = "El mes debe estar entre 1 y 12" });
            }

            if (request.Year <= 2000)
            {
                return new BadRequestObjectResult(new { error = "El año debe ser mayor a 2000" });
            }

            var budget = new MonthlyBudget
            {
                UserId = userId.Value,
                CategoryId = request.CategoryId,
                Year = request.Year,
                Month = request.Month,
                Amount = request.Amount
            };

            var saved = await _budgetRepository.UpsertAsync(budget);

            return new OkObjectResult(new BudgetResponseDto
            {
                Id = saved.Id,
                CategoryId = saved.CategoryId,
                Year = saved.Year,
                Month = saved.Month,
                Amount = saved.Amount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear/actualizar presupuesto");
            return new ObjectResult(new { error = "Error interno del servidor" }) { StatusCode = 500 };
        }
    }
}
