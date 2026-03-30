using MiDineroIA_Backend.Application.DTOs;
using MiDineroIA_Backend.Application.Exceptions;
using MiDineroIA_Backend.Domain.Entities;
using MiDineroIA_Backend.Domain.Interfaces;

namespace MiDineroIA_Backend.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;

    public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher, ITokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }



    // REGISTRAR USUARIO:
    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser is not null)
            throw new ConflictException("El email ya está registrado");

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Currency = "USD",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdUser = await _userRepository.CreateAsync(user);

        var token = _tokenGenerator.GenerateToken(createdUser);
        var userDto = new UserDto(createdUser.Id, createdUser.Name, createdUser.Email);

        return new AuthResponseDto(token, userDto);
    }


    // LOGIN USUARIO:
    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null)
            throw new UnauthorizedException("Credenciales inválidas");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Credenciales inválidas");

        if (!user.IsActive)
            throw new UnauthorizedException("La cuenta está desactivada");

        var token = _tokenGenerator.GenerateToken(user);
        var userDto = new UserDto(user.Id, user.Name, user.Email);

        return new AuthResponseDto(token, userDto);
    }


}
