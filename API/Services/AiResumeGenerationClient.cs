using System.Net;
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

        var chatPayload = new
        {
            model = config.Model,
            stream = false,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "You generate resume drafts and must return JSON only."
                },
                new
                {
                    role = "user",
                    content = prompt
                }
            }
        };

        try
        {
            var (statusCode, body) = await SendAsync(client, config.BaseUrl, chatPayload, config.ApiKey, timeoutCts.Token);

            if (!IsSuccess(statusCode))
            {
                throw new InvalidOperationException($"AI generation failed with status {(int)statusCode}: {body}");
            }

            return ExtractGeneratedJson(body);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"AI generation timed out after {timeoutSeconds} seconds.");
        }
    }

    private static async Task<(HttpStatusCode statusCode, string body)> SendAsync(
        HttpClient client,
        string url,
        object payload,
        string apiKey,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json")
        };

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
        }

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return (response.StatusCode, body);
    }

    private static bool IsSuccess(HttpStatusCode statusCode)
        => (int)statusCode is >= 200 and < 300;

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

                if (root.TryGetProperty("response", out var response) && response.ValueKind == JsonValueKind.String)
                {
                    return response.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("message", out var message)
                    && message.ValueKind == JsonValueKind.Object
                    && message.TryGetProperty("content", out var messageContent)
                    && messageContent.ValueKind == JsonValueKind.String)
                {
                    return messageContent.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("choices", out var choices)
                    && choices.ValueKind == JsonValueKind.Array
                    && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];

                    if (firstChoice.ValueKind == JsonValueKind.Object)
                    {
                        if (firstChoice.TryGetProperty("message", out var choiceMessage)
                            && choiceMessage.ValueKind == JsonValueKind.Object
                            && choiceMessage.TryGetProperty("content", out var choiceMessageContent)
                            && choiceMessageContent.ValueKind == JsonValueKind.String)
                        {
                            return choiceMessageContent.GetString() ?? string.Empty;
                        }

                        if (firstChoice.TryGetProperty("text", out var choiceText)
                            && choiceText.ValueKind == JsonValueKind.String)
                        {
                            return choiceText.GetString() ?? string.Empty;
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
        }

        return trimmed;
    }
}
