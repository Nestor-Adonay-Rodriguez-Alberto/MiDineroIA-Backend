namespace MiDineroIA_Backend.Application.Exceptions;

/// <summary>
/// Excepción para cuando un recurso no existe (404 Not Found).
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
