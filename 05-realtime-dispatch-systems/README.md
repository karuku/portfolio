# Real-Time Dispatch Systems

A .NET 10 reference service for deterministic nearest-driver matching, concurrency-safe assignment, offline driver update ingestion, and SignalR broadcasts.

## Run

```powershell
dotnet test DispatchSystems.sln
dotnet run --project src/DispatchApi
```

The API listens on `http://localhost:5090` when run from the root Compose setup. The standalone development server uses its configured ASP.NET Core launch profile. Health is available at `/health`, the SignalR hub at `/hubs/dispatch`, and HTTP resources under `/api/dispatch` and `/api/driver-updates`.

## Behavior

- `POST /api/dispatch/requests` selects the closest available driver within the requested radius. Ties are resolved by driver ID, and a lock prevents assigning one driver to concurrent requests.
- `POST /api/driver-updates` accepts the mobile outbox format, requires `Idempotency-Key` to match the event ID, and returns a replay without broadcasting a duplicate.
- New assignments and driver updates are broadcast through a strongly typed SignalR client contract.
- The in-memory update ledger is bounded to 500 recent events. Its idempotency window is process-local and ends when an event is evicted or the process restarts.

## Production boundary

The sample has no identity provider, authorization policy, durable request store, distributed idempotency store, message broker, or backplane for multi-instance SignalR. Do not expose it as a production dispatch service without those controls and a persistent assignment transaction. The matching and event contracts are kept independent so those adapters can be added without changing the domain rules.