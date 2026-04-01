using MiDineroIA_Backend.Domain.Entities;

namespace MiDineroIA_Backend.Domain.Interfaces;

/// <summary>
/// Repositorio para operaciones de mensajes del chat.
/// </summary>
public interface IChatRepository
{
    /// <summary>
    /// Guarda un mensaje de chat en la base de datos.
    /// </summary>
    /// <param name="message">Mensaje a guardar (USER_TEXT, USER_IMAGE, o AI_RESPONSE).</param>
    /// <returns>Mensaje guardado con Id asignado.</returns>
    Task<ChatMessage> SaveMessageAsync(ChatMessage message);

    /// <summary>
    /// Obtiene el historial de mensajes de un usuario con paginación.
    /// </summary>
    /// <param name="userId">Id del usuario.</param>
    /// <param name="page">Número de página (1-based).</param>
    /// <param name="pageSize">Cantidad de mensajes por página.</param>
    /// <returns>Lista de mensajes ordenados por CreatedAt DESC.</returns>
    Task<List<ChatMessage>> GetHistoryAsync(int userId, int page, int pageSize);
}
