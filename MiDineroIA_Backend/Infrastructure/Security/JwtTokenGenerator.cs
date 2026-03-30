using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MiDineroIA_Backend.Domain.Entities;
using MiDineroIA_Backend.Domain.Interfaces;

namespace MiDineroIA_Backend.Infrastructure.Security;

public class JwtTokenGenerator : ITokenGenerator
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly int _expirationDays;

    public JwtTokenGenerator()
    {
        _secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? throw new InvalidOperationException("JWT_SECRET not configured");
        _issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? throw new InvalidOperationException("JWT_ISSUER not configured");
        _expirationDays = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRATION_DAYS") ?? "7");
    }



    // GENERAR TOKEN JWT:
    public string GenerateToken(User user)
    {
        if (user is null)
            throw new ArgumentNullException(nameof(user));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _issuer,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(_expirationDays),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }


    // VALIDAR TOKEN JWT:
    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _issuer,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret)),
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = new JwtSecurityTokenHandler()
                .ValidateToken(token, parameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }


}
