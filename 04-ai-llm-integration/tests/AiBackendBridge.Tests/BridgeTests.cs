using AiBackendBridge;
using System.Net;
using System.Text.Json;

namespace AiBackendBridge.Tests;

public sealed class BridgeTests
{
    [Fact]
    public void Sanitizer_removes_control_characters_and_bounds_input()
    {
        var result = InputSanitizer.Clean("hello\0\u0007 world", 7);

        Assert.Equal("hello w", result);
    }

    [Fact]
    public async Task Handler_returns_versioned_summary()
    {
        var handler = new SummarizeHandler(new FakeLanguageModel(), new PromptCatalog());

        var result = await handler.HandleAsync("A useful document.", CancellationToken.None);

        Assert.Equal("summary.v1", result.PromptVersion);
        Assert.NotEmpty(result.Text);
    }
}

public sealed class OpenAiResponsesLanguageModelTests
{
    [Fact]
    public async Task Sends_versioned_instructions_without_storing_provider_response()
    {
        string? authorization = null;
        string? requestBody = null;
        var handler = new StubHttpMessageHandler(async (request, cancellationToken) =>
        {
            authorization = request.Headers.Authorization?.ToString();
            requestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return JsonResponse("""
                {
                  "status":"completed",
                  "output":[
                    {"type":"reasoning","content":[]},
                    {"type":"message","content":[{"type":"output_text","text":"A verified summary."}]}
                  ]
                }
                """, HttpStatusCode.Created);
        });
        using var client = new HttpClient(handler);
        var model = new OpenAiResponsesLanguageModel(client, "test-key", "gpt-4.1-mini");

        var result = await model.CompleteAsync(new ModelRequest("Be precise.", "Source text.", CancellationToken.None));

        Assert.Equal("A verified summary.", result.Content);
        Assert.Equal("Bearer test-key", authorization);
        using var requestJson = JsonDocument.Parse(requestBody!);
        Assert.Equal("gpt-4.1-mini", requestJson.RootElement.GetProperty("model").GetString());
        Assert.Equal("Be precise.", requestJson.RootElement.GetProperty("instructions").GetString());
        Assert.Equal("Source text.", requestJson.RootElement.GetProperty("input").GetString());
        Assert.Equal(2_000, requestJson.RootElement.GetProperty("max_output_tokens").GetInt32());
        Assert.False(requestJson.RootElement.GetProperty("store").GetBoolean());
    }

    [Fact]
    public async Task Does_not_include_provider_error_body_in_exception()
    {
        var handler = new StubHttpMessageHandler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent("private provider error body")
            }));
        using var client = new HttpClient(handler);
        var model = new OpenAiResponsesLanguageModel(client, "test-key", "gpt-4.1-mini");

        var error = await Assert.ThrowsAsync<HttpRequestException>(() =>
            model.CompleteAsync(new ModelRequest("system", "user", CancellationToken.None)));

        Assert.Equal(HttpStatusCode.TooManyRequests, error.StatusCode);
        Assert.DoesNotContain("private provider error body", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Uses_caller_cancellation_token()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var client = new HttpClient(new StubHttpMessageHandler(async (_, token) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return JsonResponse("{}");
        }));
        var model = new OpenAiResponsesLanguageModel(client, "test-key", "gpt-4.1-mini");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            model.CompleteAsync(new ModelRequest("system", "user", cancellation.Token)));
    }

    [Fact]
    public void Requires_provider_configuration()
    {
        using var client = new HttpClient(new StubHttpMessageHandler((_, _) =>
            Task.FromResult(JsonResponse("{}"))));

        Assert.Throws<ArgumentException>(() => new OpenAiResponsesLanguageModel(client, " ", "gpt-4.1-mini"));
        Assert.Throws<ArgumentException>(() => new OpenAiResponsesLanguageModel(client, "test-key", " "));
    }

    private static HttpResponseMessage JsonResponse(string json, HttpStatusCode status = HttpStatusCode.OK) => new(status)
    {
        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
    };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
