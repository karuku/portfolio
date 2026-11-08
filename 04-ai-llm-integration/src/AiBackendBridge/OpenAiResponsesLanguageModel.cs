using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AiBackendBridge;

public sealed class OpenAiResponsesLanguageModel : ILanguageModel
{
    private static readonly Uri ResponsesEndpoint = new("https://api.openai.com/v1/responses");
    private static readonly TimeSpan DefaultRequestTimeout = TimeSpan.FromSeconds(30);
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly TimeSpan _requestTimeout;

    public OpenAiResponsesLanguageModel(
        HttpClient httpClient,
        string apiKey,
        string model,
        TimeSpan? requestTimeout = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiKey = string.IsNullOrWhiteSpace(apiKey)
            ? throw new ArgumentException("An API key is required.", nameof(apiKey))
            : apiKey;
        _model = string.IsNullOrWhiteSpace(model)
            ? throw new ArgumentException("A model name is required.", nameof(model))
            : model;
        _requestTimeout = requestTimeout ?? DefaultRequestTimeout;
        if (_requestTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(requestTimeout), "The timeout must be positive.");
    }

    public async Task<ModelResponse> CompleteAsync(ModelRequest request)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(request.CancellationToken);
        timeout.CancelAfter(_requestTimeout);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ResponsesEndpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        httpRequest.Content = JsonContent.Create(new ResponsesApiRequest(
            _model,
            request.System,
            request.User,
            MaxOutputTokens: 2_000,
            Store: false));

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            timeout.Token);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"The model provider returned HTTP {(int)response.StatusCode}.",
                inner: null,
                response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<ResponsesApiResult>(cancellationToken: timeout.Token);
        if (result?.Status != "completed" || result.Output is null)
            throw new InvalidOperationException("The model provider returned an incomplete response.");

        var text = string.Join(
            Environment.NewLine,
            result.Output
                .Where(item => item.Type == "message")
                .SelectMany(item => item.Content ?? [])
                .Where(item => item.Type == "output_text")
                .Select(item => item.Text)
                .Where(value => !string.IsNullOrWhiteSpace(value)));

        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("The model provider returned no text output.");

        return new ModelResponse(text);
    }

    private sealed record ResponsesApiRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("instructions")] string Instructions,
        [property: JsonPropertyName("input")] string Input,
        [property: JsonPropertyName("max_output_tokens")] int MaxOutputTokens,
        [property: JsonPropertyName("store")] bool Store);

    private sealed record ResponsesApiResult(
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("output")] IReadOnlyList<ResponsesOutputItem> Output);

    private sealed record ResponsesOutputItem(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("content")] IReadOnlyList<ResponsesOutputContent>? Content);

    private sealed record ResponsesOutputContent(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string? Text);
}
