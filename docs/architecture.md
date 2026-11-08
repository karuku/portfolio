# Architecture decision record

## Why this repository is modular

The portfolio is structured as small, independently readable reference modules. This keeps examples focused while showing how the same engineering principles transfer between a .NET backend, a TypeScript frontend, and an AI integration.

## Boundary rules

1. The domain owns business language and invariants.
2. Application code orchestrates use cases through ports.
3. Infrastructure implements ports and may depend on external SDKs.
4. Presentation translates transport concerns into application requests and responses.
5. Tests at each boundary prefer deterministic fakes over network calls.

These are deliberately lightweight rules: the goal is to create useful seams, not ceremony.
