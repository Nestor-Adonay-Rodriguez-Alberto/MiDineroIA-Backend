namespace MiDineroIA_Backend.Domain.Interfaces;

public interface IPasswordHasher
{
    /// <summary>
    /// Genera un hash seguro de la contraseña en texto plano.
    /// </summary>
    /// <param name="rawPassword">Contraseña sin encriptar</param>
    /// <returns>Hash seguro de la contraseña</returns>
    string Hash(string rawPassword);

    /// <summary>
    /// Verifica si la contraseña en texto plano coincide con el hash almacenado.
    /// </summary>
    /// <param name="rawPassword">Contraseña sin encriptar</param>
    /// <param name="hash">Hash almacenado en la base de datos</param>
    /// <returns>true si las contraseñas coinciden, false en caso contrario</returns>
    bool Verify(string rawPassword, string hash);
}
