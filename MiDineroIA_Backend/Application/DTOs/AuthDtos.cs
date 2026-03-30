namespace MiDineroIA_Backend.Application.DTOs;

public record RegisterRequestDto(string Name, string Email, string Password);

public record LoginRequestDto(string Email, string Password);

public record UserDto(int Id, string Name, string Email);

public record AuthResponseDto(string Token, UserDto User);
