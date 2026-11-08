using DispatchApi.Domain;
using DispatchApi.Endpoints;
using DispatchApi.Infrastructure;
using DispatchApi.Realtime;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddValidation();
builder.Services.AddSignalR();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton(new DispatchMatcher(
[
    new DriverProfile("DRIVER-07", new GeoPoint(-1.2864, 36.8172)),
    new DriverProfile("DRIVER-12", new GeoPoint(-1.2921, 36.8219)),
    new DriverProfile("DRIVER-19", new GeoPoint(-1.2798, 36.8126))
]));
builder.Services.AddSingleton<DriverUpdateLedger>();
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("driver-updates", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var app = builder.Build();
app.UseExceptionHandler();
app.UseRouting();
app.UseRateLimiter();
app.MapHealthChecks("/health");
app.MapHub<DispatchHub>("/hubs/dispatch");
app.MapDispatchEndpoints();

app.Run();

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled dispatch API error for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await Results.Problem(
            statusCode: httpContext.Response.StatusCode,
            title: "An unexpected error occurred.",
            extensions: new Dictionary<string, object?> { ["traceId"] = httpContext.TraceIdentifier })
            .ExecuteAsync(httpContext);
        return true;
    }
}
