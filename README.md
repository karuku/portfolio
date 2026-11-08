# Hi, I'm Robert Karuku 👋

### Senior Full-Stack & LLM Engineer · C#/.NET · React · TypeScript

I design and build dependable products across the whole stack: well-factored .NET services, accessible React interfaces, and practical AI integrations that solve real business problems. My work is grounded in clean boundaries, observable systems, and delivery practices that make software easier to change.

[![Portfolio site](https://img.shields.io/badge/portfolio-view%20website-17241f)](https://karuku.github.io/portfolio/)
[![CI](https://github.com/karuku/portfolio/actions/workflows/ci.yml/badge.svg)](https://github.com/karuku/portfolio/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## What I do

- Build APIs with C#, ASP.NET Core, Minimal APIs, Dapper, EF Core, SQL Server, PostgreSQL, Redis, and MongoDB.
- Ship responsive web and mobile experiences with React, React Native (Expo), TypeScript, Vite, Tailwind CSS, and Next.js.
- Apply Clean Architecture, DDD, CQRS, SOLID, event-driven workflows, and pragmatic testing.
- Integrate LLMs safely with versioned prompts, structured outputs, resilient clients, and human-review boundaries.
- Improve delivery with Docker, GitHub Actions, Azure DevOps, telemetry, and repeatable environments.

## Portfolio modules

This repository is an executable engineering portfolio—not a collection of screenshots. Each module is intentionally small enough to study and opinionated enough to discuss in an interview.

| Module | What it demonstrates |
| --- | --- |
| [`architectural-patterns`](architectural-patterns/) | A .NET 10 Clean Architecture reference API with CQRS-style handlers, validation, problem details, structured logging, and tests. |
| [`react-patterns`](react-patterns/) | Framework-agnostic TypeScript patterns for API clients, JWT refresh coordination, polling, and accessible async UI state. |
| [`ai-backend-bridge`](ai-backend-bridge/) | A provider-agnostic .NET LLM gateway with prompt versioning, structured output, timeouts, and safe input cleansing. |

## Engineering principles

```mermaid
flowchart LR
    UI[React / TypeScript] --> API[HTTP API]
    API --> APP[Application use cases]
    APP --> DOMAIN[Domain rules]
    APP --> INFRA[Infrastructure adapters]
    INFRA --> DB[(Database)]
    APP --> AI[LLM gateway]
    API --> OBS[Logs · metrics · traces]
```

- Dependencies point inward; domain code does not know about frameworks.
- External systems are adapters behind small interfaces.
- Invalid input is rejected at the boundary, while domain invariants remain enforceable everywhere.
- AI output is treated as untrusted data and validated before it reaches a business workflow.
- Every production path should be observable, testable, and boring to operate.

## Selected experience

I have 6+ years of experience delivering business software, including law-firm management and vehicle-hailing workflows. I enjoy the seams between product, architecture, and implementation: turning ambiguous requirements into systems that teams can confidently extend.

## Connect

- [GitHub](https://github.com/karuku)

Open to thoughtful conversations about full-stack engineering, platform work, and responsible LLM integrations.

## Local development

The repository is designed to be approachable without hidden setup. Start with the module README that matches what you want to explore. To run the full validation locally:

```powershell
dotnet test architectural-patterns/ArchitecturalPatterns.sln
dotnet test ai-backend-bridge/AiBackendBridge.sln
```

GitHub Actions runs the .NET tests and formatting checks, TypeScript compilation, a Docker Compose API smoke test, and scheduled CodeQL analysis for C# and TypeScript.

To exercise the API in a container:

```powershell
docker compose up --build
Invoke-RestMethod http://localhost:5080/health
```

## Production notes

The examples are production-shaped reference implementations, not pretend SaaS deployments. Before using one as a product foundation, add an external persistence adapter, authentication/authorization, request rate limits, distributed telemetry, secret management, and environment-specific deployment manifests. The boundaries are deliberately in place so those concerns can be added without rewriting the domain or use cases.
