using System.Security.Claims;

namespace MiDineroIA_Backend.Domain.Interfaces;

public interface ITokenValidator
{
    ClaimsPrincipal? ValidateToken(string token);
}
