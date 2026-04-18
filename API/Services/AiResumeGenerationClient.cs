using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace API.Services;

public class AiResumeGenerationClient : IAiResumeGenerationClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<AiServiceOptions> _options;

    public AiResumeGenerationClient(IHttpClientFactory httpClientFactory, IOptions<AiServiceOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
    }

    public async Task<string> GenerateResumeJsonAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var config = _options.Value;
        if (string.IsNullOrWhiteSpace(config.BaseUrl))
        {
            throw new InvalidOperationException("AiService:BaseUrl is not configured.");
        }

        if (string.IsNullOrWhiteSpace(config.Model))
        {
            throw new InvalidOperationException("AiService:Model is not configured.");
        }

        var timeoutSeconds = config.TimeoutSeconds <= 0 ? 60 : config.TimeoutSeconds;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        var client = _httpClientFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, config.BaseUrl)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    model = config.Model,
                    prompt
                }),
                Encoding.UTF8,
                "application/json")
        };

        if (!string.IsNullOrWhiteSpace(config.ApiKey))
        {
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {config.ApiKey}");
        }

        try
        {
            using var response = await client.SendAsync(request, timeoutCts.Token);
            var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"AI generation failed with status {(int)response.StatusCode}: {body}");
            }

            return ExtractGeneratedJson(body);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"AI generation timed out after {timeoutSeconds} seconds.");
        }
    }

    private static string ExtractGeneratedJson(string responseBody)
    {
        var trimmed = responseBody.Trim();

        try
        {
            using var document = JsonDocument.Parse(trimmed);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("generatedResumeJson", out var generated) && generated.ValueKind == JsonValueKind.String)
                {
                    return generated.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.String)
                {
                    return output.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
                {
                    return content.GetString() ?? string.Empty;
                }
            }
        }
        catch (JsonException)
        {
        }

        return trimmed;
    }
}
