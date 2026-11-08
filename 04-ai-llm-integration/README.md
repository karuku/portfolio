# AI backend bridge

An intentionally small .NET service boundary for LLM features. The application owns prompt selection and output validation; a provider adapter owns HTTP and credentials. This prevents SDK details from leaking into business code and makes tests deterministic.

## Design

- `PromptCatalog` versions prompts as code and exposes only approved versions.
- `InputSanitizer` removes control characters and bounds input length before it is sent externally.
- `ILanguageModel` is the provider port; `FakeLanguageModel` keeps tests and local demos deterministic.
- `OpenAiResponsesLanguageModel` is an opt-in `HttpClient` adapter for OpenAI's Responses API. It sets an output-token ceiling, honors cancellation with a request timeout, disables response storage, and never reads or logs provider error bodies.
- `SummarizeHandler` validates the provider response and returns a typed result.

Run the tests with:

```powershell
dotnet test AiBackendBridge.sln
```

To use the HTTP adapter, inject an `HttpClient`, a model identifier, and `OPENAI_API_KEY` from environment configuration or a secret manager. The library never reads secrets from source files. A live provider call was intentionally not made in CI; tests use a fake handler and do not require credentials.

The sample doesn't implement retries because retrying a billable generation automatically can duplicate cost. A real application should add an explicit retry policy for safe transient failures, request-level budget limits, moderation and human review appropriate to the workflow, and telemetry that records metadata rather than prompts or personal data. OpenAI may change models and prices; pin and evaluate model versions in the consuming application.
