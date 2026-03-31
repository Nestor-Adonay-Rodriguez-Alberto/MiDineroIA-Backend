using MiDineroIA_Backend.Application.DTOs;

namespace MiDineroIA_Backend.Application.Interfaces;

/// <summary>
/// Servicio orquestador del chat con IA.
/// Procesa mensajes del usuario, interactúa con Claude, y ejecuta acciones según la intención.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Procesa un mensaje del usuario y devuelve la respuesta del sistema.
    /// </summary>
    /// <param name="userId">Id del usuario autenticado.</param>
    /// <param name="message">Mensaje de texto del usuario.</param>
    /// <param name="imageBase64">Imagen en base64 (opcional, para facturas). Ignorado en Fase 2.</param>
    /// <returns>Respuesta con intent, datos de acción, y mensaje para el usuario.</returns>
    Task<ChatResponseDto> ProcessMessageAsync(int userId, string message, string? imageBase64);

    /// <summary>
    /// Obtiene el historial de mensajes del chat de un usuario con paginación.
    /// </summary>
    /// <param name="userId">Id del usuario.</param>
    /// <param name="page">Número de página (1-based).</param>
    /// <param name="pageSize">Cantidad de mensajes por página.</param>
    /// <returns>Lista de mensajes ordenados por fecha descendente.</returns>
    Task<List<ChatMessageDto>> GetHistoryAsync(int userId, int page, int pageSize);
}
