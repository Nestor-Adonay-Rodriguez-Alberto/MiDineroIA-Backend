using Microsoft.EntityFrameworkCore;
using MiDineroIA_Backend.Domain.Entities;
using MiDineroIA_Backend.Domain.Interfaces;
using MiDineroIA_Backend.Infrastructure.Database;

namespace MiDineroIA_Backend.Infrastructure.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly AppDbContext _context;

    public ChatRepository(AppDbContext context)
    {
        _context = context;
    }



    // GUARDA UN MENSAJE DE CHAT EN LA BASE DE DATOS:
    public async Task<ChatMessage> SaveMessageAsync(ChatMessage message)
    {
        message.CreatedAt = DateTime.UtcNow;
        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();
        return message;
    }


    // OBTIENE EL HISTORIAL DE MENSAJES DE UN USUARIO CON PAGINACIÓN:
    public async Task<List<ChatMessage>> GetHistoryAsync(int userId, int page, int pageSize)
    {
        return await _context.ChatMessages
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

}
