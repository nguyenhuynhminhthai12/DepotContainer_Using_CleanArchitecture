using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TechSpherex.CleanArchitecture.Application.Abstractions.Agents;

namespace TechSpherex.CleanArchitecture.Infrastructure.Agents;

/// <summary>
/// Triển khai dịch vụ gọi Google Gemini REST API (gemini-3.6-flash / gemini-2.5-flash).
/// Sử dụng JsonDocument parsing linh hoạt, tương thích 100% với cấu trúc phản hồi của Google.
/// </summary>
public sealed class GeminiAIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiAIService> _logger;
    private readonly string? _apiKey;
    private readonly string _primaryModel;
    private readonly string _fallbackModel;

    public GeminiAIService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiAIService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Gemini:ApiKey"]?.Trim();
        _primaryModel = configuration["Gemini:Model"]?.Trim() ?? "gemini-3.6-flash";
        _fallbackModel = configuration["Gemini:FallbackModel"]?.Trim() ?? "gemini-2.5-flash";

        var timeoutSec = configuration.GetValue("Gemini:TimeoutSeconds", 30);
        _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSec);
    }

    /// <inheritdoc/>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    /// <inheritdoc/>
    public async Task<string?> GenerateResponseAsync(
        string systemInstruction,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Gemini API key is not configured.");
            return null;
        }

        // Thử model chính trước
        var response = await CallGeminiApiAsync(_primaryModel, systemInstruction, userPrompt, cancellationToken);
        if (!string.IsNullOrWhiteSpace(response))
            return response;

        // Nếu model chính lỗi và có fallback khác model chính, thử fallback
        if (!string.Equals(_primaryModel, _fallbackModel, StringComparison.OrdinalIgnoreCase))
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Retrying Gemini request with fallback model: {FallbackModel}", _fallbackModel);
            }
            response = await CallGeminiApiAsync(_fallbackModel, systemInstruction, userPrompt, cancellationToken);
        }

        return response;
    }

    private async Task<string?> CallGeminiApiAsync(
        string model,
        string systemInstruction,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_apiKey}";
        var payload = BuildRequestBody(systemInstruction, userPrompt);

        try
        {
            var jsonString = JsonSerializer.Serialize(payload);
            using var httpContent = new StringContent(jsonString, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, httpContent, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini API call ({Model}) failed with status {StatusCode}: {Error}", model, response.StatusCode, responseBody);
                return null;
            }

            var resultText = ExtractTextFromResponse(responseBody);
            if (!string.IsNullOrWhiteSpace(resultText))
            {
                return resultText;
            }

            _logger.LogWarning("Gemini response did not contain candidates/parts text: {Body}", responseBody);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while invoking Gemini model {Model}", model);
            return null;
        }
    }

    private static Dictionary<string, object?> BuildRequestBody(string systemInstruction, string userPrompt)
    {
        var payload = new Dictionary<string, object?>
        {
            ["contents"] = new object[]
            {
                new
                {
                    role = "user",
                    parts = new object[]
                    {
                        new { text = userPrompt }
                    }
                }
            },
            ["generationConfig"] = new
            {
                temperature = 0.3,
                maxOutputTokens = 2048
            }
        };

        if (!string.IsNullOrWhiteSpace(systemInstruction))
        {
            payload["system_instruction"] = new
            {
                parts = new object[]
                {
                    new { text = systemInstruction }
                }
            };
        }

        return payload;
    }

    private static string? ExtractTextFromResponse(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
            return null;
        }

        var firstCandidate = candidates[0];
        if (!firstCandidate.TryGetProperty("content", out var content) ||
            !content.TryGetProperty("parts", out var parts) ||
            parts.GetArrayLength() == 0)
        {
            return null;
        }

        var textParts = new StringBuilder();
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var textElem))
            {
                var textVal = textElem.GetString();
                if (!string.IsNullOrWhiteSpace(textVal))
                {
                    textParts.Append(textVal);
                }
            }
        }

        var result = textParts.ToString().Trim();
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }
}
