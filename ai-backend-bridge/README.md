# AI backend bridge

An intentionally small .NET service boundary for LLM features. The application owns prompt selection and output validation; a provider adapter owns HTTP and credentials. This prevents SDK details from leaking into business code and makes tests deterministic.

## Design

- `PromptCatalog` versions prompts as code and exposes only approved versions.
- `InputSanitizer` removes control characters and bounds input length before it is sent externally.
- `ILanguageModel` is the provider port; `FakeLanguageModel` makes the sample runnable without a key.
- `SummarizeHandler` validates the provider response and returns a typed result.

Run the tests with:

```powershell
dotnet test AiBackendBridge.sln
```

In a real deployment, implement `ILanguageModel` with an official provider SDK, inject the API key from a secret store, add request-level cost/rate limits, and log metadata—not prompts or personal data.
