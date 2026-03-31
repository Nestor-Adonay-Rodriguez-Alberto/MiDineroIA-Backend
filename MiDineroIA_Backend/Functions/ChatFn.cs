using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MiDineroIA_Backend.Application.DTOs;
using MiDineroIA_Backend.Application.Interfaces;
using MiDineroIA_Backend.CrossCutting.Auth;
using MiDineroIA_Backend.Domain.Interfaces;

namespace MiDineroIA_Backend.Functions;

public class ChatFn
{
    private readonly IChatService _chatService;
    private readonly ITokenValidator _tokenValidator;
    private readonly ILogger<ChatFn> _logger;

    public ChatFn(IChatService chatService, ITokenValidator tokenValidator, ILogger<ChatFn> logger)
    {
        _chatService = chatService;
        _tokenValidator = tokenValidator;
        _logger = logger;
    }




    /// <summary>
    /// POST /api/chat — Procesa un mensaje del usuario con IA.
    /// </summary>
    [Function("ChatProcess")]
    public async Task<IActionResult> ProcessMessage([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "chat")] HttpRequest req)
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
            var request = await req.ReadFromJsonAsync<ChatRequestDto>();
            if (request is null || string.IsNullOrWhiteSpace(request.Message))
            {
                return new BadRequestObjectResult(new { error = "El mensaje es requerido" });
            }

            // Procesar mensaje
            var result = await _chatService.ProcessMessageAsync(userId.Value, request.Message, request.ImageBase64);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando mensaje de chat");
            return new ObjectResult(new { error = "Error interno del servidor" }) { StatusCode = 500 };
        }
    }


    /// <summary>
    /// GET /api/chat/history — Obtiene el historial de mensajes del usuario.
    /// </summary>
    [Function("ChatHistory")]
    public async Task<IActionResult> GetHistory([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "chat/history")] HttpRequest req)
    {
        try
        {
            // Validar JWT y extraer userId
            var userId = AuthHelper.ExtractUserIdFromRequest(req, _tokenValidator);
            if (userId is null)
            {
                return new UnauthorizedObjectResult(new { error = "Token inválido o expirado" });
            }

            // Obtener parámetros de paginación
            var pageStr = req.Query["page"].FirstOrDefault();
            var pageSizeStr = req.Query["pageSize"].FirstOrDefault();

            var page = int.TryParse(pageStr, out var p) && p > 0 ? p : 1;
            var pageSize = int.TryParse(pageSizeStr, out var ps) && ps > 0 && ps <= 100 ? ps : 20;

            // Obtener historial
            var messages = await _chatService.GetHistoryAsync(userId.Value, page, pageSize);
            return new OkObjectResult(new { messages, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo historial de chat");
            return new ObjectResult(new { error = "Error interno del servidor" }) { StatusCode = 500 };
        }
    }


}
