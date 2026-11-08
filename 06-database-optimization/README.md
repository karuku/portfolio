# Database Optimization Lab

A .NET 10 API demonstrating Dapper with PostgreSQL, bounded parameterized queries, deterministic keyset pagination, and a covering index chosen for the access path.

## Run

From the repository root:

```powershell
$env:PORTFOLIO_DB_PASSWORD = "local-only-change-this-value"
docker compose up --build
Invoke-RestMethod "http://localhost:5091/api/orders?customerId=11111111-1111-4111-8111-111111111111&pageSize=10"
```

PostgreSQL is available on host port `5433`, bound to loopback; the API is on `5091`. Compose requires `PORTFOLIO_DB_PASSWORD` and has no checked-in default. Use a disposable local value here, and use a secret manager for deployed environments. The initialization script seeds deterministic demo rows on first database creation.

## Query design

- The repository selects a bounded `pageSize + 1` rows and uses the extra row to determine whether another page exists.
- Pages use `(created_at, id) < (@BeforeCreatedAt, @BeforeId)` with a stable descending order, avoiding the increasing scan cost of large offsets.
- `sql/init.sql` creates `ix_orders_customer_created_id` on `(customer_id, created_at DESC, id DESC)` and includes the response columns for the demonstrated query.
- Every client value is a Dapper parameter. SQL structure is selected only from two fixed query strings.
- `CancellationToken` flows into Npgsql through Dapper's `CommandDefinition`.

Use the returned `nextCursor.createdAt` and `nextCursor.id` as the `beforeCreatedAt` and `beforeId` query parameters for the next page. Keep page sizes from 1 to 100. To check the actual planner choice, run `EXPLAIN (ANALYZE, BUFFERS)` against the repository query with representative row counts; an index definition alone does not guarantee a good plan.

## Limits

This is a performance reference, not a production data service. Add authentication and tenant isolation, migrations with an upgrade/rollback policy, connection and command timeouts, telemetry, backup/restore procedures, and load tests against representative data before deployment. `sql/init.sql` is only for first-run local seeding.