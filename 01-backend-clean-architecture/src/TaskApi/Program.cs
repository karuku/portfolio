using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskApi.Application;
using TaskApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddSingleton<ITaskRepository, InMemoryTaskRepository>();
builder.Services.AddScoped<CreateTaskHandler>();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.UseExceptionHandler();
app.MapHealthChecks("/health").AllowAnonymous();

app.MapGet("/tasks", async (ITaskRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.ListAsync(cancellationToken)));

app.MapPost("/tasks", async (CreateTaskRequest request, CreateTaskHandler handler, CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(request, cancellationToken);
    return Results.Created($"/tasks/{result.Id}", result);
});

app.Run();

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        var isValidationError = exception is ValidationException;
        httpContext.Response.StatusCode = isValidationError ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError;
        await Results.Problem(statusCode: httpContext.Response.StatusCode, title: isValidationError ? exception.Message : "An unexpected error occurred.", extensions: new Dictionary<string, object?>
        {
            ["traceId"] = httpContext.TraceIdentifier
        }).ExecuteAsync(httpContext);
        return true;
    }
}
