using MiDineroIA_Backend.Domain.Entities;

namespace MiDineroIA_Backend.Domain.Interfaces;

public interface ITokenGenerator
{
    string GenerateToken(User user);
}
