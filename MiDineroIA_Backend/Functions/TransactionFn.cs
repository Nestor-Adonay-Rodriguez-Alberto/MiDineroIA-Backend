using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MiDineroIA_Backend.Application.DTOs;
using MiDineroIA_Backend.CrossCutting.Auth;
using MiDineroIA_Backend.Domain.Interfaces;

namespace MiDineroIA_Backend.Functions;

/// <summary>
/// Azure Functions para operaciones sobre transacciones.
/// </summary>
public class TransactionFn
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITokenValidator _tokenValidator;
    private readonly ILogger<TransactionFn> _logger;

    public TransactionFn(ITransactionRepository transactionRepository, ITokenValidator tokenValidator, ILogger<TransactionFn> logger)
    {
        _transactionRepository = transactionRepository;
        _tokenValidator = tokenValidator;
        _logger = logger;
    }




    /// <summary>
    /// PUT /api/transactions/{id}/confirm — Confirma una transacción pendiente.
    /// </summary>
    [Function("TransactionConfirm")]
    public async Task<IActionResult> Confirm([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "transactions/{id}/confirm")] HttpRequest req,int id)
    {
        try
        {
            // Validar JWT y extraer userId
            var userId = AuthHelper.ExtractUserIdFromRequest(req, _tokenValidator);
            if (userId is null)
            {
                return new UnauthorizedObjectResult(new { error = "Token inválido o expirado" });
            }

            // Confirmar transacción (el repositorio ya filtra por userId)
            var confirmed = await _transactionRepository.ConfirmAsync(id, userId.Value);
            if (!confirmed)
            {
                return new NotFoundObjectResult(new { error = "Transacción no encontrada" });
            }

            // Obtener la transacción actualizada
            var transaction = await _transactionRepository.GetByIdAsync(id, userId.Value);
            
            return new OkObjectResult(new TransactionResponseDto
            {
                Id = transaction!.Id,
                CategoryId = transaction.CategoryId,
                CategoryName = transaction.Category?.Name,
                Amount = transaction.Amount,
                Description = transaction.Description,
                Merchant = transaction.Merchant,
                TransactionDate = transaction.TransactionDate,
                IsConfirmed = transaction.IsConfirmed
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirmando transacción {TransactionId}", id);
            return new ObjectResult(new { error = "Error interno del servidor" }) { StatusCode = 500 };
        }
    }


    /// <summary>
    /// PUT /api/transactions/{id} — Actualiza una transacción y la marca como confirmada.
    /// </summary>
    [Function("TransactionUpdate")]
    public async Task<IActionResult> Update([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "transactions/{id}")] HttpRequest req, int id)
    {
        try
        {
            // Validar JWT y extraer userId
            var userId = AuthHelper.ExtractUserIdFromRequest(req, _tokenValidator);
            if (userId is null)
            {
                return new UnauthorizedObjectResult(new { error = "Token inválido o expirado" });
            }

            // Leer request body
            var request = await req.ReadFromJsonAsync<UpdateTransactionRequestDto>();
            if (request is null)
            {
                return new BadRequestObjectResult(new { error = "Request body inválido" });
            }

            if (request.Amount <= 0)
            {
                return new BadRequestObjectResult(new { error = "El monto debe ser mayor a 0" });
            }

            // Obtener transacción existente
            var transaction = await _transactionRepository.GetByIdAsync(id, userId.Value);
            if (transaction is null)
            {
                return new NotFoundObjectResult(new { error = "Transacción no encontrada" });
            }

            // Actualizar campos
            transaction.CategoryId = request.CategoryId;
            transaction.Amount = request.Amount;
            transaction.Description = request.Description;
            transaction.IsConfirmed = true;

            var updated = await _transactionRepository.UpdateAsync(transaction);

            // Recargar para obtener la categoría
            var reloaded = await _transactionRepository.GetByIdAsync(id, userId.Value);

            return new OkObjectResult(new TransactionResponseDto
            {
                Id = reloaded!.Id,
                CategoryId = reloaded.CategoryId,
                CategoryName = reloaded.Category?.Name,
                Amount = reloaded.Amount,
                Description = reloaded.Description,
                Merchant = reloaded.Merchant,
                TransactionDate = reloaded.TransactionDate,
                IsConfirmed = reloaded.IsConfirmed
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando transacción {TransactionId}", id);
            return new ObjectResult(new { error = "Error interno del servidor" }) { StatusCode = 500 };
        }
    }


    /// <summary>
    /// DELETE /api/transactions/{id} — Elimina una transacción.
    /// </summary>
    [Function("TransactionDelete")]
    public async Task<IActionResult> Delete([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "transactions/{id}")] HttpRequest req,int id)
    {
        try
        {
            // Validar JWT y extraer userId
            var userId = AuthHelper.ExtractUserIdFromRequest(req, _tokenValidator);
            if (userId is null)
            {
                return new UnauthorizedObjectResult(new { error = "Token inválido o expirado" });
            }

            // Eliminar transacción (el repositorio ya filtra por userId)
            var deleted = await _transactionRepository.DeleteAsync(id, userId.Value);
            if (!deleted)
            {
                return new NotFoundObjectResult(new { error = "Transacción no encontrada" });
            }

            return new OkObjectResult(new { message = "Transacción eliminada correctamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando transacción {TransactionId}", id);
            return new ObjectResult(new { error = "Error interno del servidor" }) { StatusCode = 500 };
        }
    }


}
