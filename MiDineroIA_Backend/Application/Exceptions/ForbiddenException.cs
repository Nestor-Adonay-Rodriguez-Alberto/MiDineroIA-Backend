namespace MiDineroIA_Backend.Application.Exceptions;

/// <summary>
/// Excepción para cuando el usuario no tiene permiso para acceder a un recurso (403 Forbidden).
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
