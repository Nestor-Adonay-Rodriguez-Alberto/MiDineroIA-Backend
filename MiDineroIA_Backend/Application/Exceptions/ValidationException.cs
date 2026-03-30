namespace MiDineroIA_Backend.Application.Exceptions;

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
