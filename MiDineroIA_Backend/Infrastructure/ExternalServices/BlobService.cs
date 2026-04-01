using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using MiDineroIA_Backend.Domain.Interfaces;

namespace MiDineroIA_Backend.Infrastructure.ExternalServices;

public class BlobService : IBlobService
{
    private readonly BlobContainerClient _containerClient;
    private readonly string _connectionString;

    private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".webp", "image/webp" }
    };

    public BlobService()
    {
        _connectionString = Environment.GetEnvironmentVariable("BLOB_CONNECTION_STRING") ?? throw new InvalidOperationException("BLOB_CONNECTION_STRING no está configurado");

        var containerName = Environment.GetEnvironmentVariable("BLOB_CONTAINER_NAME") ?? "receipts";

        var blobServiceClient = new BlobServiceClient(_connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
    }



    public async Task<string> UploadImageAsync(int userId, byte[] imageBytes, string fileName)
    {
        // Validar que hay datos
        if (imageBytes == null || imageBytes.Length == 0)
        {
            throw new ArgumentException("La imagen está vacía.");
        }

        // Validar tamaño máximo
        if (imageBytes.Length > MaxFileSizeBytes)
        {
            throw new ArgumentException($"La imagen excede el tamaño máximo permitido de 5 MB. Tamaño actual: {imageBytes.Length / (1024 * 1024.0):F2} MB");
        }

        // Validar extensión
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant() ?? "";
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"Formato de imagen no válido. Solo se permiten: {string.Join(", ", AllowedExtensions)}");
        }

        // Generar nombre único: user_{userId}/{fecha}_{guid}.{ext}
        var blobName = $"user_{userId}/{DateTime.UtcNow:yyyy-MM-dd}_{Guid.NewGuid()}{extension}";

        // Obtener blob client
        var blobClient = _containerClient.GetBlobClient(blobName);

        // Configurar headers
        var blobHttpHeaders = new BlobHttpHeaders
        {
            ContentType = ContentTypes.GetValueOrDefault(extension, "application/octet-stream")
        };

        // Subir imagen
        using var stream = new MemoryStream(imageBytes);
        await blobClient.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = blobHttpHeaders
        });

        return blobClient.Uri.ToString();
    }

 
    public Task<string> GetSasUrlAsync(string blobUrl)
    {
        // Extraer el nombre del blob de la URL
        var uri = new Uri(blobUrl);
        var blobName = uri.AbsolutePath.TrimStart('/');
        
        // Remover el nombre del contenedor del path
        var containerName = _containerClient.Name;
        if (blobName.StartsWith($"{containerName}/", StringComparison.OrdinalIgnoreCase))
        {
            blobName = blobName.Substring(containerName.Length + 1);
        }

        var blobClient = _containerClient.GetBlobClient(blobName);

        // Verificar que podemos generar SAS (necesitamos account key en connection string)
        if (!blobClient.CanGenerateSasUri)
        {
            throw new InvalidOperationException("No se puede generar SAS URI. Verifica que la connection string incluye AccountKey.");
        }

        // Generar SAS token con 30 minutos de expiración
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerClient.Name,
            BlobName = blobName,
            Resource = "b", // "b" = blob
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(30)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasUrl = blobClient.GenerateSasUri(sasBuilder).ToString();

        return Task.FromResult(sasUrl);
    }


}
