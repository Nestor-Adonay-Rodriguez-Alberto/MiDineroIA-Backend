using MiDineroIA_Backend.Application.DTOs;
using MiDineroIA_Backend.Application.Exceptions;
using MiDineroIA_Backend.Application.Mapping;
using MiDineroIA_Backend.CrossCutting.Auth;
using MiDineroIA_Backend.Domain.Entities;
using MiDineroIA_Backend.Domain.Interfaces;

namespace MiDineroIA_Backend.Application.Services;

public class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly JwtHelper _jwtHelper;
    private readonly UserMapper _userMapper;

    public AuthService(IUserRepository userRepository, JwtHelper jwtHelper,UserMapper userMapper)
    {
        _userRepository = userRepository;
        _jwtHelper = jwtHelper;
        _userMapper = userMapper;
    }



    // REGISTRAR USUARIO:
    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser is not null)
            throw new ConflictException("El email ya esta registrado");

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await _userRepository.CreateAsync(user);

        var token = _jwtHelper.GenerateToken(user);
        var userDto = _userMapper.ToUserDto(user);

        return new AuthResponseDto(token, userDto);
    }


    // LOGIN USUARIO:
    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null)
            throw new UnauthorizedException("Credenciales invalidas");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Credenciales invalidas");

        var token = _jwtHelper.GenerateToken(user);
        var userDto = _userMapper.ToUserDto(user);

        return new AuthResponseDto(token, userDto);
    }

}
