using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using MiDineroIA_Backend.Domain.Interfaces;

namespace MiDineroIA_Backend.CrossCutting.Auth;

/// <summary>
/// Helper para extraer y validar información de autenticación de las requests HTTP.
/// </summary>
public static class AuthHelper
{
    /// <summary>
    /// Extrae el userId del token JWT en el header Authorization.
    /// </summary>
    /// <param name="request">HTTP request con el header Authorization: Bearer {token}.</param>
    /// <param name="tokenValidator">Validador de tokens JWT.</param>
    /// <returns>userId si el token es válido, null si no hay token o es inválido.</returns>
    public static int? ExtractUserIdFromRequest(HttpRequest request, ITokenValidator tokenValidator)
    {
        var authHeader = request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = authHeader["Bearer ".Length..].Trim();
        var principal = tokenValidator.ValidateToken(token);
        
        if (principal is null)
        {
            return null;
        }

        var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return userId;
    }
}
