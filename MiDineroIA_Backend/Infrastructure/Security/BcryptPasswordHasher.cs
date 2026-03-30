using MiDineroIA_Backend.Domain.Interfaces;

namespace MiDineroIA_Backend.Infrastructure.Security;

public class BcryptPasswordHasher : IPasswordHasher
{
    // HASH CONTRASEÑA:
    public string Hash(string rawPassword)
    {
        if (string.IsNullOrWhiteSpace(rawPassword))
            throw new ArgumentException("La contraseña no puede estar vacía", nameof(rawPassword));

        return BCrypt.Net.BCrypt.HashPassword(rawPassword);
    }


    // VERIFICAR CONTRASEÑA:
    public bool Verify(string rawPassword, string hash)
    {
        if (string.IsNullOrWhiteSpace(rawPassword) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(rawPassword, hash);
        }
        catch
        {
            return false;
        }
    }

}
