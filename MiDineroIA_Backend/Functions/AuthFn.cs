using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MiDineroIA_Backend.Application.DTOs;
using MiDineroIA_Backend.Application.Exceptions;
using MiDineroIA_Backend.Application.Services;

namespace MiDineroIA_Backend.Functions;

public class AuthFn
{
    private readonly AuthService _authService;
    private readonly ILogger<AuthFn> _logger;

    public AuthFn(AuthService authService, ILogger<AuthFn> logger)
    {
        _authService = authService;
        _logger = logger;
    }




    [Function("AuthRegister")]
    public async Task<IActionResult> Register([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/register")] HttpRequest req)
    {
        var body = await req.ReadFromJsonAsync<RegisterRequestDto>();
        if (body is null)
            return new BadRequestObjectResult(new { error = "Request body invalido" });

        try
        {
            var result = await _authService.RegisterAsync(body);
            return new OkObjectResult(result);
        }
        catch (ConflictException ex)
        {
            return new ConflictObjectResult(new { error = ex.Message });
        }
    }


    [Function("AuthLogin")]
    public async Task<IActionResult> Login([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] HttpRequest req)
    {
        var body = await req.ReadFromJsonAsync<LoginRequestDto>();
        if (body is null)
            return new BadRequestObjectResult(new { error = "Request body invalido" });

        try
        {
            var result = await _authService.LoginAsync(body);
            return new OkObjectResult(result);
        }
        catch (UnauthorizedException ex)
        {
            return new UnauthorizedObjectResult(new { error = ex.Message });
        }
    }

}
