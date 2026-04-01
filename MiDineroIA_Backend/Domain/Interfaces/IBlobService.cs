namespace MiDineroIA_Backend.Domain.Interfaces;

/// <summary>
/// Servicio para manejo de imágenes en Azure Blob Storage.
/// </summary>
public interface IBlobService
{
    /// <summary>
    /// Sube una imagen al Blob Storage.
    /// </summary>
    /// <param name="userId">Id del usuario propietario.</param>
    /// <param name="imageBytes">Bytes de la imagen.</param>
    /// <param name="fileName">Nombre original del archivo (para obtener extensión).</param>
    /// <returns>URL del blob subido.</returns>
    /// <exception cref="ArgumentException">Si la imagen excede 5MB o no es formato válido.</exception>
    Task<string> UploadImageAsync(int userId, byte[] imageBytes, string fileName);

    /// <summary>
    /// Genera una URL con SAS token para acceso temporal de lectura.
    /// </summary>
    /// <param name="blobUrl">URL del blob.</param>
    /// <returns>URL con SAS token (válido por 30 minutos).</returns>
    Task<string> GetSasUrlAsync(string blobUrl);
}
