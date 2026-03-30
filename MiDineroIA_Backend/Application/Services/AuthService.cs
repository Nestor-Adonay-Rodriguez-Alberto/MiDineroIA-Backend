using System.Text.RegularExpressions;
using MiDineroIA_Backend.Application.DTOs;
using MiDineroIA_Backend.Application.Exceptions;
using MiDineroIA_Backend.Domain.Entities;
using MiDineroIA_Backend.Application.Interfaces;
using MiDineroIA_Backend.Domain.Interfaces;

namespace MiDineroIA_Backend.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;

    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher, ITokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }



    // REGISTRAR USUARIO:
    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        ValidateRegisterRequest(request);

        var email = request.Email.Trim().ToLowerInvariant();

        var existingUser = await _userRepository.GetByEmailAsync(email);
        if (existingUser is not null)
            throw new ConflictException("El email ya está registrado");

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
        };

        var createdUser = await _userRepository.CreateAsync(user);

        var token = _tokenGenerator.GenerateToken(createdUser);
        var userDto = new UserDto(createdUser.Id, createdUser.Name, createdUser.Email);

        return new AuthResponseDto(token, userDto);
    }


    // LOGIN USUARIO:
    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        ValidateLoginRequest(request);

        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _userRepository.GetByEmailAsync(email);
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


    // VALIDACIONES PRIVADAS:
    private static void ValidateRegisterRequest(RegisterRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("El nombre es requerido");

        if (request.Name.Trim().Length < 2)
            throw new ValidationException("El nombre debe tener mínimo 2 caracteres");

        if (request.Name.Trim().Length > 100)
            throw new ValidationException("El nombre no puede exceder 100 caracteres");

        ValidateEmail(request.Email);
        ValidatePassword(request.Password);
    }

    private static void ValidateLoginRequest(LoginRequestDto request)
    {
        ValidateEmail(request.Email);

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ValidationException("La contraseña es requerida");
    }

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ValidationException("El email es requerido");

        if (!EmailRegex.IsMatch(email.Trim()))
            throw new ValidationException("El formato del email es inválido");
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ValidationException("La contraseña es requerida");

        if (password.Length < 8)
            throw new ValidationException("La contraseña debe tener mínimo 8 caracteres");
    }
}
