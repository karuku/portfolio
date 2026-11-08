using DatabaseApi;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Orders");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Configure ConnectionStrings__Orders before starting the API.");

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
builder.Services.AddScoped<OrderRepository>();

var app = builder.Build();

app.MapGet("/health", async (NpgsqlDataSource dataSource, CancellationToken cancellationToken) =>
{
    await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
    return TypedResults.Ok(new { status = "Healthy", database = "Connected" });
});

app.MapGet("/api/orders", async Task<Results<Ok<OrderPageResponse>, BadRequest<ProblemDetails>>> (
    Guid customerId,
    int? pageSize,
    DateTimeOffset? beforeCreatedAt,
    Guid? beforeId,
    OrderRepository repository,
    CancellationToken cancellationToken) =>
{
    if (!OrderPageQuery.TryCreate(customerId, pageSize, beforeCreatedAt, beforeId, out var query))
    {
        return TypedResults.BadRequest(new ProblemDetails
        {
            Title = "The customer ID, page size, or cursor is invalid.",
            Status = StatusCodes.Status400BadRequest
        });
    }

    return TypedResults.Ok(await repository.ListAsync(query, cancellationToken));
})
.WithName("ListCustomerOrders");

app.Run();
