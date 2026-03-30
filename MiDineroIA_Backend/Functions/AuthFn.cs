using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MiDineroIA_Backend.Application.DTOs;
using MiDineroIA_Backend.Application.Exceptions;
using MiDineroIA_Backend.Domain.Interfaces;

namespace MiDineroIA_Backend.Functions;

public class AuthFn
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthFn> _logger;

    public AuthFn(IAuthService authService, ILogger<AuthFn> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [Function("AuthRegister")]
    public async Task<IActionResult> Register(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/register")] HttpRequest req)
    {
        try
        {
            var request = await req.ReadFromJsonAsync<RegisterRequestDto>();
            if (request is null)
                return new BadRequestObjectResult(new { error = "Request body inválido" });

            var result = await _authService.RegisterAsync(request);
            return new OkObjectResult(result);
        }
        catch (ValidationException ex)
        {
            return new BadRequestObjectResult(new { error = ex.Message });
        }
        catch (ConflictException ex)
        {
            return new ConflictObjectResult(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en registro de usuario");
            return new ObjectResult(new { error = "Error interno del servidor" }) { StatusCode = 500 };
        }
    }

    [Function("AuthLogin")]
    public async Task<IActionResult> Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] HttpRequest req)
    {
        try
        {
            var request = await req.ReadFromJsonAsync<LoginRequestDto>();
            if (request is null)
                return new BadRequestObjectResult(new { error = "Request body inválido" });

            var result = await _authService.LoginAsync(request);
            return new OkObjectResult(result);
        }
        catch (ValidationException ex)
        {
            return new BadRequestObjectResult(new { error = ex.Message });
        }
        catch (UnauthorizedException ex)
        {
            return new UnauthorizedObjectResult(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en login de usuario");
            return new ObjectResult(new { error = "Error interno del servidor" }) { StatusCode = 500 };
        }
    }
}
