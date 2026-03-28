using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace MiDineroIA_Backend.CrossCutting.Auth;

public static class JwtAuthHelper
{
    public static int? GetUserIdFromRequest(HttpRequest req, JwtHelper jwtHelper)
    {
        var authHeader = req.Headers["Authorization"].FirstOrDefault();
        if (authHeader is null || !authHeader.StartsWith("Bearer "))
            return null;

        var token = authHeader.Substring("Bearer ".Length).Trim();
        var principal = jwtHelper.ValidateToken(token);
        if (principal is null)
            return null;

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)
                       ?? principal.FindFirst(JwtRegisteredClaimNames.Sub);
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
            return null;

        return userId;
    }
}
