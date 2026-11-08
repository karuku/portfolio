using DispatchApi.Domain;
using DispatchApi.Infrastructure;
using DispatchApi.Realtime;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace DispatchApi.Endpoints;

public static class DispatchEndpoints
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.Ordinal)
    {
        "accepted",
        "en-route",
        "arrived",
        "picked-up",
        "completed"
    };

    public static IEndpointRouteBuilder MapDispatchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var dispatch = endpoints.MapGroup("/api/dispatch").WithTags("Dispatch");

        dispatch.MapPost("/requests", async Task<Results<Ok<DispatchAssignmentResponse>, Conflict<ProblemDetails>>> (
            CreateDispatchRequest request,
            DispatchMatcher matcher,
            IHubContext<DispatchHub, IDispatchClient> hub,
            CancellationToken cancellationToken) =>
        {
            DispatchAssignment? assignment;
            try
            {
                assignment = matcher.TryAssign(new DispatchRequest(
                    request.RequestId,
                    request.PickupLabel,
                    new GeoPoint(request.Latitude, request.Longitude),
                    request.MaximumDistanceKm));
            }
            catch (DispatchRequestConflictException)
            {
                return TypedResults.Conflict(new ProblemDetails
                {
                    Title = "The dispatch request ID was already used with a different payload.",
                    Status = StatusCodes.Status409Conflict
                });
            }

            if (assignment is null)
            {
                return TypedResults.Conflict(new ProblemDetails
                {
                    Title = "No available driver is within the requested radius.",
                    Status = StatusCodes.Status409Conflict
                });
            }

            var response = ToResponse(assignment);
            await hub.Clients.All.AssignmentCreated(response);
            return TypedResults.Ok(response);
        })
        .WithName("CreateDispatchRequest");

        dispatch.MapGet("/assignments", (DispatchMatcher matcher) =>
            TypedResults.Ok(matcher.ListAssignments().Select(ToResponse).ToArray()))
            .WithName("ListDispatchAssignments");

        dispatch.MapGet("/assignments/{id}", Results<Ok<DispatchAssignmentResponse>, NotFound> (string id, DispatchMatcher matcher) =>
            matcher.FindAssignment(id) is { } assignment
                ? TypedResults.Ok(ToResponse(assignment))
                : TypedResults.NotFound())
            .WithName("GetDispatchAssignment");

        endpoints.MapPost("/api/driver-updates", async Task<Results<Created<DriverUpdateResponse>, Ok<DriverUpdateResponse>, BadRequest<ProblemDetails>, Conflict<ProblemDetails>>> (
            CreateDriverUpdateRequest request,
            HttpContext httpContext,
            DriverUpdateLedger ledger,
            IHubContext<DispatchHub, IDispatchClient> hub,
            CancellationToken cancellationToken) =>
        {
            var idempotencyKey = httpContext.Request.Headers["Idempotency-Key"].ToString();
            if (string.IsNullOrWhiteSpace(idempotencyKey) || !string.Equals(idempotencyKey, request.Id, StringComparison.Ordinal))
            {
                return TypedResults.BadRequest(new ProblemDetails
                {
                    Title = "The Idempotency-Key header must match the event ID.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (!AllowedStatuses.Contains(request.Status) || request.OccurredAt == default)
            {
                return TypedResults.BadRequest(new ProblemDetails
                {
                    Title = "The update status or occurrence time is invalid.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var update = new DriverUpdate(request.Id, request.TripId, request.Status, request.OccurredAt);
            var receipt = ledger.Record(update);
            if (receipt.Status == UpdateReceiptStatus.Conflict)
            {
                return TypedResults.Conflict(new ProblemDetails
                {
                    Title = "The idempotency key was already used for a different update.",
                    Status = StatusCodes.Status409Conflict
                });
            }

            var response = ToResponse(receipt.Update);
            if (receipt.Status == UpdateReceiptStatus.Replay)
                return TypedResults.Ok(response);

            await hub.Clients.All.DriverUpdateReceived(response);
            return TypedResults.Created($"/api/driver-updates/{Uri.EscapeDataString(response.Id)}", response);
        })
        .WithName("CreateDriverUpdate")
        .RequireRateLimiting("driver-updates");

        endpoints.MapGet("/api/driver-updates", (DriverUpdateLedger ledger) =>
            TypedResults.Ok(ledger.ListRecent().Select(ToResponse).ToArray()))
            .WithName("ListDriverUpdates");

        endpoints.MapGet("/api/driver-updates/{id}", Results<Ok<DriverUpdateResponse>, NotFound> (string id, DriverUpdateLedger ledger) =>
            ledger.Find(id) is { } update
                ? TypedResults.Ok(ToResponse(update))
                : TypedResults.NotFound())
            .WithName("GetDriverUpdate");

        return endpoints;
    }

    private static DispatchAssignmentResponse ToResponse(DispatchAssignment assignment) =>
        new(assignment.Id, assignment.RequestId, assignment.PickupLabel, assignment.DriverId, Math.Round(assignment.DistanceKm, 3), assignment.AssignedAt);

    private static DriverUpdateResponse ToResponse(DriverUpdate update) =>
        new(update.Id, update.TripId, update.Status, update.OccurredAt);
}
