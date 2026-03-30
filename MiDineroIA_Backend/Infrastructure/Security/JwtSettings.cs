namespace MiDineroIA_Backend.Infrastructure.Security;

public class JwtSettings
{
    public string Secret { get; }
    public string Issuer { get; }
    public int ExpirationDays { get; }

    public JwtSettings(string secret, string issuer, int expirationDays)
    {
        Secret = secret;
        Issuer = issuer;
        ExpirationDays = expirationDays;
    }

    public static JwtSettings FromEnvironment()
    {
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? throw new InvalidOperationException("JWT_SECRET not configured");
        var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? throw new InvalidOperationException("JWT_ISSUER not configured");
        var expirationDays = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRATION_DAYS") ?? "7");

        return new JwtSettings(secret, issuer, expirationDays);
    }
}
