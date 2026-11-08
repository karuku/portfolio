using System.ComponentModel.DataAnnotations;

namespace DispatchApi.Domain;

public sealed record GeoPoint(double Latitude, double Longitude);

public sealed record DriverProfile(string Id, GeoPoint Location);

public sealed record DispatchRequest(string Id, string PickupLabel, GeoPoint Pickup, double MaximumDistanceKm);

public sealed record DispatchAssignment(
    string Id,
    string RequestId,
    string PickupLabel,
    string DriverId,
    double DistanceKm,
    DateTimeOffset AssignedAt);

public sealed record DriverUpdate(string Id, string TripId, string Status, DateTimeOffset OccurredAt);

public enum UpdateReceiptStatus
{
    Created,
    Replay,
    Conflict
}

public sealed record UpdateReceipt(UpdateReceiptStatus Status, DriverUpdate Update);

/// <summary>Payload for requesting a demo driver assignment.</summary>
public sealed record CreateDispatchRequest
{
    [Required, StringLength(64)]
    public required string RequestId { get; init; }

    [Required, StringLength(80)]
    public required string PickupLabel { get; init; }

    [Range(-90, 90)]
    public required double Latitude { get; init; }

    [Range(-180, 180)]
    public required double Longitude { get; init; }

    [Range(0.1, 100)]
    public double MaximumDistanceKm { get; init; } = 15;
}

/// <summary>Payload for an offline-queued driver status event.</summary>
public sealed record CreateDriverUpdateRequest
{
    [Required, StringLength(64)]
    public required string Id { get; init; }

    [Required, StringLength(64)]
    public required string TripId { get; init; }

    [Required, StringLength(24)]
    public required string Status { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }
}

public sealed record DispatchAssignmentResponse(
    string Id,
    string RequestId,
    string PickupLabel,
    string DriverId,
    double DistanceKm,
    DateTimeOffset AssignedAt);

public sealed record DriverUpdateResponse(
    string Id,
    string TripId,
    string Status,
    DateTimeOffset OccurredAt);

public sealed class DispatchRequestConflictException(string requestId)
    : Exception($"Dispatch request '{requestId}' was already used with a different payload.");
