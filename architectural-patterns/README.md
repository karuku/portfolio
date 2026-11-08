# Clean Architecture reference API

This sample models a small task service using a vertical slice inside a Clean Architecture boundary. It intentionally uses an in-memory repository so the example can run without provisioning a database; replacing the adapter with Dapper or EF Core does not change the use case.

## Structure

```text
src/TaskApi
├── Domain       entities and invariants
├── Application  commands, ports, validation, and handlers
├── Infrastructure in-memory adapters and logging
└── Presentation HTTP endpoints and problem details
tests/TaskApi.Tests
```

## Run

```powershell
dotnet run --project src/TaskApi
Invoke-RestMethod http://localhost:5080/tasks
```

The API exposes `POST /tasks`, `GET /tasks`, and `GET /health`. Errors use RFC 9457 problem details. The sample keeps the write path explicit and testable; production persistence, authentication, and telemetry can be added behind the existing interfaces.
