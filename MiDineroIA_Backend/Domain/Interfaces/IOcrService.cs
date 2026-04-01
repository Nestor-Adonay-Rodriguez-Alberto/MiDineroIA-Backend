namespace MiDineroIA_Backend.Domain.Interfaces;

/// <summary>
/// Servicio para extracción de texto de imágenes mediante OCR.
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// Extrae texto de una imagen usando Azure Computer Vision OCR.
    /// </summary>
    /// <param name="imageUrl">URL de la imagen (debe ser accesible públicamente o con SAS token).</param>
    /// <returns>Texto extraído de la imagen.</returns>
    /// <exception cref="InvalidOperationException">Si el OCR falla o no puede extraer texto suficiente.</exception>
    Task<string> ExtractTextAsync(string imageUrl);
}
