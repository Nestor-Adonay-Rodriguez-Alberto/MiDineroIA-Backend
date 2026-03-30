using MiDineroIA_Backend.Application.DTOs;

namespace MiDineroIA_Backend.Domain.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
}
