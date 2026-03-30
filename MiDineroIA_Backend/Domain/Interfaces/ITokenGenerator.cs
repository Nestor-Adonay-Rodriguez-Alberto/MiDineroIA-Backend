using System.Security.Claims;
using MiDineroIA_Backend.Domain.Entities;

namespace MiDineroIA_Backend.Domain.Interfaces;

public interface ITokenGenerator
{
    /// <summary>
    /// Genera un token JWT para autenticación.
    /// </summary>
    /// <param name="user">Usuario autenticado</param>
    /// <returns>Token JWT válido</returns>
    string GenerateToken(User user);

    /// <summary>
    /// Valida un token JWT y extrae sus claims.
    /// </summary>
    /// <param name="token">Token a validar</param>
    /// <returns>Claims del token si es válido, null si es inválido</returns>
    ClaimsPrincipal? ValidateToken(string token);
}
