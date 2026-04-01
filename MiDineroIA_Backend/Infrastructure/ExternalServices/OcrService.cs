using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MiDineroIA_Backend.Domain.Interfaces;

namespace MiDineroIA_Backend.Infrastructure.ExternalServices;

/// <summary>
/// Servicio de OCR usando Azure Computer Vision Read API.
/// </summary>
public class OcrService : IOcrService
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _apiKey;

    private const int MaxPollingAttempts = 10;
    private const int PollingDelayMs = 1000;
    private const int MinTextLength = 10;

    public OcrService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _apiKey = Environment.GetEnvironmentVariable("OCR_KEY") ?? throw new InvalidOperationException("OCR_KEY no está configurado");
        _endpoint = Environment.GetEnvironmentVariable("OCR_ENDPOINT") ?? throw new InvalidOperationException("OCR_ENDPOINT no está configurado");

        // Asegurar que el endpoint no termine con /
        _endpoint = _endpoint.TrimEnd('/');
    }


    public async Task<string> ExtractTextAsync(string imageUrl)
    {
        // 1. Iniciar análisis
        var operationLocation = await StartAnalysisAsync(imageUrl);

        // 2. Polling hasta obtener resultado
        var result = await PollForResultAsync(operationLocation);

        // 3. Extraer texto de las líneas
        var extractedText = ExtractTextFromResult(result);

        // 4. Validar que hay suficiente texto
        if (string.IsNullOrWhiteSpace(extractedText) || extractedText.Length < MinTextLength)
        {
            throw new InvalidOperationException(
                "No se pudo extraer texto suficiente de la imagen. " +
                "Asegúrate de que la imagen sea clara y contenga texto legible.");
        }

        return extractedText;
    }


    private async Task<string> StartAnalysisAsync(string imageUrl)
    {
        var analyzeUrl = $"{_endpoint}/vision/v3.2/read/analyze";

        var requestBody = new { url = imageUrl };
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, analyzeUrl);
        request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
        request.Content = jsonContent;

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Error al iniciar análisis OCR: {response.StatusCode}. {errorContent}");
        }

        // Obtener Operation-Location del header
        if (!response.Headers.TryGetValues("Operation-Location", out var operationLocations))
        {
            throw new InvalidOperationException(
                "La respuesta de OCR no contiene el header Operation-Location");
        }

        return operationLocations.First();
    }


    private async Task<OcrReadResult> PollForResultAsync(string operationLocation)
    {
        for (int attempt = 0; attempt < MaxPollingAttempts; attempt++)
        {
            await Task.Delay(PollingDelayMs);

            using var request = new HttpRequestMessage(HttpMethod.Get, operationLocation);
            request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"Error al obtener resultado OCR: {response.StatusCode}. {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OcrReadResult>(content);

            if (result == null)
            {
                throw new InvalidOperationException("Respuesta OCR inválida");
            }

            switch (result.Status?.ToLowerInvariant())
            {
                case "succeeded":
                    return result;
                case "failed":
                    throw new InvalidOperationException(
                        "El análisis OCR falló. La imagen puede estar dañada o no ser legible.");
                case "running":
                case "notstarted":
                    // Continuar polling
                    continue;
                default:
                    throw new InvalidOperationException($"Estado OCR desconocido: {result.Status}");
            }
        }

        throw new InvalidOperationException(
            $"Timeout: El análisis OCR no completó después de {MaxPollingAttempts} intentos.");
    }


    private static string ExtractTextFromResult(OcrReadResult result)
    {
        if (result.AnalyzeResult?.ReadResults == null)
        {
            return string.Empty;
        }

        var lines = new List<string>();

        foreach (var readResult in result.AnalyzeResult.ReadResults)
        {
            if (readResult.Lines == null) continue;

            foreach (var line in readResult.Lines)
            {
                if (!string.IsNullOrWhiteSpace(line.Text))
                {
                    lines.Add(line.Text);
                }
            }
        }

        return string.Join("\n", lines);
    }


    // DTOs para deserializar la respuesta de Azure Vision

    private class OcrReadResult
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("analyzeResult")]
        public AnalyzeResult? AnalyzeResult { get; set; }
    }

    private class AnalyzeResult
    {
        [JsonPropertyName("readResults")]
        public List<ReadResult>? ReadResults { get; set; }
    }

    private class ReadResult
    {
        [JsonPropertyName("lines")]
        public List<OcrLine>? Lines { get; set; }
    }

    private class OcrLine
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
