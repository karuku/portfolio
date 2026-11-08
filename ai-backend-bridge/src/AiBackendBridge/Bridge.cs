using System.Text;

namespace AiBackendBridge;

public sealed record Prompt(string Name, int Version, string System, string UserTemplate);
public sealed record ModelRequest(string System, string User, CancellationToken CancellationToken);
public sealed record ModelResponse(string Content);
public sealed record Summary(string Text, string PromptVersion);

public interface ILanguageModel
{
    Task<ModelResponse> CompleteAsync(ModelRequest request);
}

public sealed class PromptCatalog
{
    private static readonly Prompt SummaryV1 = new(
        "summary", 1,
        "Summarize the supplied text accurately. Do not invent facts.",
        "Text to summarize:\n{input}");

    public Prompt GetSummaryPrompt() => SummaryV1;
}

public static class InputSanitizer
{
    public static string Clean(string input, int maxCharacters = 8_000)
    {
        ArgumentNullException.ThrowIfNull(input);
        var builder = new StringBuilder(Math.Min(input.Length, maxCharacters));
        foreach (var character in input)
        {
            if (!char.IsControl(character) || character is '\n' or '\r' or '\t')
                builder.Append(character);
            if (builder.Length == maxCharacters)
                break;
        }
        return builder.ToString().Trim();
    }
}

public sealed class SummarizeHandler(ILanguageModel model, PromptCatalog prompts)
{
    public async Task<Summary> HandleAsync(string input, CancellationToken cancellationToken)
    {
        var cleanInput = InputSanitizer.Clean(input);
        if (cleanInput.Length == 0)
            throw new ArgumentException("Input must contain readable text.", nameof(input));

        var prompt = prompts.GetSummaryPrompt();
        var request = new ModelRequest(prompt.System, prompt.UserTemplate.Replace("{input}", cleanInput, StringComparison.Ordinal), cancellationToken);
        var response = await model.CompleteAsync(request);
        var summary = response.Content.Trim();
        if (summary.Length == 0 || summary.Length > 2_000)
            throw new InvalidOperationException("The model returned an invalid summary.");

        return new Summary(summary, $"{prompt.Name}.v{prompt.Version}");
    }
}

public sealed class FakeLanguageModel : ILanguageModel
{
    public Task<ModelResponse> CompleteAsync(ModelRequest request) =>
        Task.FromResult(new ModelResponse("A concise summary would be produced here by the configured provider."));
}
