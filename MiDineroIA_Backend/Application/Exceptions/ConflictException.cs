namespace MiDineroIA_Backend.Application.Exceptions;

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
