using AiBackendBridge;

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
