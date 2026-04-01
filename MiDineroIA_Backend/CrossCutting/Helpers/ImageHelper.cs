namespace MiDineroIA_Backend.CrossCutting.Helpers;

/// <summary>
/// Helper para validación y procesamiento de imágenes.
/// </summary>
public static class ImageHelper
{
    private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    // Magic bytes para detectar formato de imagen
    private static readonly byte[] JpegMagicBytes = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] PngMagicBytes = { 0x89, 0x50, 0x4E, 0x47 };
    private static readonly byte[] WebpMagicBytes = { 0x52, 0x49, 0x46, 0x46 }; // "RIFF"

    /// <summary>
    /// Valida y decodifica una imagen en base64.
    /// </summary>
    /// <param name="base64String">String base64 de la imagen (puede incluir prefijo data:image/...).</param>
    /// <returns>Tupla con los bytes de la imagen y la extensión detectada.</returns>
    /// <exception cref="ArgumentException">Si la imagen es inválida, muy grande o formato no soportado.</exception>
    public static (byte[] Bytes, string Extension) ValidateAndDecodeBase64(string base64String)
    {
        if (string.IsNullOrWhiteSpace(base64String))
        {
            throw new ArgumentException("La imagen está vacía.");
        }

        // Remover prefijo data:image/... si existe
        var base64Data = RemoveDataUriPrefix(base64String);

        // Decodificar base64
        byte[] imageBytes;
        try
        {
            imageBytes = Convert.FromBase64String(base64Data);
        }
        catch (FormatException)
        {
            throw new ArgumentException("La imagen no tiene un formato base64 válido.");
        }

        // Validar tamaño
        if (imageBytes.Length == 0)
        {
            throw new ArgumentException("La imagen está vacía.");
        }

        if (imageBytes.Length > MaxFileSizeBytes)
        {
            var sizeMb = imageBytes.Length / (1024.0 * 1024.0);
            throw new ArgumentException($"La imagen excede el tamaño máximo de 5 MB. Tamaño actual: {sizeMb:F2} MB");
        }

        // Detectar formato por magic bytes
        var extension = DetectImageFormat(imageBytes);
        if (extension == null)
        {
            throw new ArgumentException("Formato de imagen no soportado. Solo se permiten: JPG, PNG, WEBP.");
        }

        return (imageBytes, extension);
    }

    /// <summary>
    /// Genera un nombre de archivo único para la imagen.
    /// </summary>
    public static string GenerateFileName(string extension)
    {
        return $"receipt_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{extension}";
    }

    private static string RemoveDataUriPrefix(string base64String)
    {
        // Formato típico: data:image/jpeg;base64,/9j/4AAQ...
        const string base64Marker = ";base64,";
        var markerIndex = base64String.IndexOf(base64Marker, StringComparison.OrdinalIgnoreCase);
        
        if (markerIndex >= 0)
        {
            return base64String.Substring(markerIndex + base64Marker.Length);
        }

        // Si no hay prefijo, asumir que es base64 puro
        return base64String;
    }

    private static string? DetectImageFormat(byte[] imageBytes)
    {
        if (imageBytes.Length < 4)
        {
            return null;
        }

        // Verificar JPEG (FF D8 FF)
        if (imageBytes.Length >= 3 &&
            imageBytes[0] == JpegMagicBytes[0] &&
            imageBytes[1] == JpegMagicBytes[1] &&
            imageBytes[2] == JpegMagicBytes[2])
        {
            return ".jpg";
        }

        // Verificar PNG (89 50 4E 47)
        if (imageBytes.Length >= 4 &&
            imageBytes[0] == PngMagicBytes[0] &&
            imageBytes[1] == PngMagicBytes[1] &&
            imageBytes[2] == PngMagicBytes[2] &&
            imageBytes[3] == PngMagicBytes[3])
        {
            return ".png";
        }

        // Verificar WEBP (RIFF....WEBP)
        if (imageBytes.Length >= 12 &&
            imageBytes[0] == WebpMagicBytes[0] &&
            imageBytes[1] == WebpMagicBytes[1] &&
            imageBytes[2] == WebpMagicBytes[2] &&
            imageBytes[3] == WebpMagicBytes[3] &&
            imageBytes[8] == 0x57 && // 'W'
            imageBytes[9] == 0x45 && // 'E'
            imageBytes[10] == 0x42 && // 'B'
            imageBytes[11] == 0x50)   // 'P'
        {
            return ".webp";
        }

        return null;
    }
}
