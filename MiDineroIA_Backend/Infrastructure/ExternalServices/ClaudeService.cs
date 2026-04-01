using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MiDineroIA_Backend.Application.DTOs;
using MiDineroIA_Backend.CrossCutting.Prompts;
using MiDineroIA_Backend.Domain.Interfaces;

namespace MiDineroIA_Backend.Infrastructure.ExternalServices;

/// <summary>
/// Implementación de IClaudeService usando la API de Anthropic.
/// </summary>
public class ClaudeService : IClaudeService
{
    private const string AnthropicApiUrl = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";
    private const int MaxTokens = 1024;
    private const int MaxRetries = 1;

    private readonly HttpClient _httpClient;
    private readonly ILogger<ClaudeService> _logger;
    private readonly string _model;
    private readonly string _apiKey;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ClaudeService(HttpClient httpClient, ILogger<ClaudeService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY") ?? throw new InvalidOperationException("CLAUDE_API_KEY environment variable is not set");
        _model = Environment.GetEnvironmentVariable("CLAUDE_MODEL") ?? "claude-haiku-4-5-20251001";
    }




    public async Task<ClaudeResponseDto> ProcessUserMessageAsync(string message, string categoriesJson, string currentDate)
    {
        var systemPrompt = SystemPrompt.Text
            .Replace("{current_date}", currentDate)
            .Replace("{categories_json}", categoriesJson);

        return await SendMessageAsync(systemPrompt, message);
    }

    public async Task<ClaudeResponseDto> ProcessReceiptOcrAsync(string ocrText, string categoriesJson, string currentDate)
    {
        var systemPrompt = OcrPrompt.Text
            .Replace("{ocr_text}", ocrText)
            .Replace("{current_date}", currentDate)
            .Replace("{categories_json}", categoriesJson);

        return await SendMessageAsync(systemPrompt, "Procesa esta factura y extrae los datos de la compra.");
    }

    private async Task<ClaudeResponseDto> SendMessageAsync(string systemPrompt, string userMessage)
    {
        var requestBody = new
        {
            model = _model,
            max_tokens = MaxTokens,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = userMessage }
            }
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        
        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, AnthropicApiUrl)
                {
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };

                request.Headers.Add("x-api-key", _apiKey);
                request.Headers.Add("anthropic-version", AnthropicVersion);

                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Claude API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                    throw new HttpRequestException($"Claude API returned {response.StatusCode}: {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var claudeApiResponse = JsonSerializer.Deserialize<ClaudeApiResponse>(responseContent, JsonOptions);

                if (claudeApiResponse?.Content == null || claudeApiResponse.Content.Count == 0)
                {
                    throw new InvalidOperationException("Claude API returned empty content");
                }

                var textContent = claudeApiResponse.Content
                    .FirstOrDefault(c => c.Type == "text")?.Text;

                if (string.IsNullOrEmpty(textContent))
                {
                    throw new InvalidOperationException("Claude API returned no text content");
                }

                // Claude should return a JSON object directly
                var result = ParseClaudeResponse(textContent);
                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse Claude response on attempt {Attempt}", attempt + 1);
                
                if (attempt == MaxRetries)
                {
                    _logger.LogError("All retry attempts exhausted for parsing Claude response");
                    return CreateErrorResponse("No pude procesar la respuesta. Por favor, intenta reformular tu mensaje.");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling Claude API");
                return CreateErrorResponse("No pude conectar con el servicio de IA. Por favor, intenta de nuevo.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing Claude response");
                
                if (attempt == MaxRetries)
                {
                    return CreateErrorResponse("Ocurrió un error inesperado. Por favor, intenta de nuevo.");
                }
            }
        }

        return CreateErrorResponse("No pude procesar tu mensaje. Por favor, intenta de nuevo.");
    }

    private ClaudeResponseDto ParseClaudeResponse(string text)
    {
        // Remove potential markdown code fences if Claude wraps JSON
        var cleanedText = text.Trim();
        if (cleanedText.StartsWith("```json"))
        {
            cleanedText = cleanedText[7..];
        }
        else if (cleanedText.StartsWith("```"))
        {
            cleanedText = cleanedText[3..];
        }
        
        if (cleanedText.EndsWith("```"))
        {
            cleanedText = cleanedText[..^3];
        }
        
        cleanedText = cleanedText.Trim();

        var result = JsonSerializer.Deserialize<ClaudeResponseDto>(cleanedText, JsonOptions);
        
        if (result == null)
        {
            throw new JsonException("Failed to deserialize Claude response to ClaudeResponseDto");
        }

        return result;
    }

    private static ClaudeResponseDto CreateErrorResponse(string message)
    {
        var errorData = JsonSerializer.SerializeToElement(new { query_type = "UNCLEAR" });
        
        return new ClaudeResponseDto
        {
            Intent = "GENERAL_QUERY",
            Data = errorData,
            Message = message,
            NeedsConfirmation = false
        };
    }

    /// <summary>
    /// Response structure from Claude API.
    /// </summary>
    private class ClaudeApiResponse
    {
        public List<ContentBlock> Content { get; set; } = new();
        public string? StopReason { get; set; }
    }

    private class ContentBlock
    {
        public string Type { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}
