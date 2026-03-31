using MiDineroIA_Backend.Application.DTOs;

namespace MiDineroIA_Backend.Domain.Interfaces;

/// <summary>
/// Servicio para procesar mensajes usando Claude API.
/// </summary>
public interface IClaudeService
{
    /// <summary>
    /// Procesa un mensaje de texto del usuario usando el system prompt principal.
    /// </summary>
    /// <param name="message">Mensaje del usuario en lenguaje natural.</param>
    /// <param name="categoriesJson">JSON con las categorías disponibles para el usuario.</param>
    /// <param name="currentDate">Fecha actual en formato YYYY-MM-DD.</param>
    /// <returns>Respuesta estructurada de Claude con intent, data y mensaje.</returns>
    Task<ClaudeResponseDto> ProcessUserMessageAsync(string message, string categoriesJson, string currentDate);

    /// <summary>
    /// Procesa texto OCR de una factura/recibo usando el prompt especializado.
    /// </summary>
    /// <param name="ocrText">Texto extraído por Azure Vision OCR.</param>
    /// <param name="categoriesJson">JSON con las categorías disponibles para el usuario.</param>
    /// <param name="currentDate">Fecha actual en formato YYYY-MM-DD.</param>
    /// <returns>Respuesta estructurada de Claude con datos de la transacción extraída.</returns>
    Task<ClaudeResponseDto> ProcessReceiptOcrAsync(string ocrText, string categoriesJson, string currentDate);
}
