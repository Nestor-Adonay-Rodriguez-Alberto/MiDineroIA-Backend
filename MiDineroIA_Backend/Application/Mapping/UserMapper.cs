using MiDineroIA_Backend.Application.DTOs;
using MiDineroIA_Backend.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace MiDineroIA_Backend.Application.Mapping;

[Mapper]
public partial class UserMapper
{
    [MapperIgnoreSource(nameof(User.PasswordHash))]
    [MapperIgnoreSource(nameof(User.MonthlyIncome))]
    [MapperIgnoreSource(nameof(User.Currency))]
    [MapperIgnoreSource(nameof(User.IsActive))]
    [MapperIgnoreSource(nameof(User.CreatedAt))]
    [MapperIgnoreSource(nameof(User.UpdatedAt))]
    [MapperIgnoreSource(nameof(User.Categories))]
    [MapperIgnoreSource(nameof(User.MonthlyBudgets))]
    [MapperIgnoreSource(nameof(User.ChatMessages))]
    [MapperIgnoreSource(nameof(User.Transactions))]
    public partial UserDto ToUserDto(User user);
}
