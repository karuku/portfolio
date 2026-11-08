using DispatchApi.Domain;
using DispatchApi.Infrastructure;

namespace DispatchApi.Tests;

public sealed class DispatchMatcherTests
{
    [Fact]
    public void Assigns_nearest_available_driver_within_radius()
    {
        var matcher = new DispatchMatcher(
        [
            new DriverProfile("driver-far", new GeoPoint(-1.30, 36.83)),
            new DriverProfile("driver-near", new GeoPoint(-1.291, 36.822))
        ]);

        var assignment = matcher.TryAssign(new DispatchRequest("request-1", "Central Station", new GeoPoint(-1.292, 36.821), 5));

        Assert.NotNull(assignment);
        Assert.Equal("driver-near", assignment.DriverId);
        Assert.Equal("Central Station", assignment.PickupLabel);
        Assert.InRange(assignment.DistanceKm, 0, 5);
    }

    [Fact]
    public async Task A_driver_is_assigned_to_only_one_concurrent_request()
    {
        var matcher = new DispatchMatcher([new DriverProfile("driver-1", new GeoPoint(-1.29, 36.82))]);

        var results = await Task.WhenAll(
            Task.Run(() => matcher.TryAssign(new DispatchRequest("request-1", "Central", new GeoPoint(-1.291, 36.821), 5))),
            Task.Run(() => matcher.TryAssign(new DispatchRequest("request-2", "West", new GeoPoint(-1.292, 36.822), 5))));

        Assert.Single(results, result => result is not null);
        Assert.Single(matcher.ListAssignments());
    }

    [Fact]
    public void Repeating_a_request_returns_the_original_assignment()
    {
        var matcher = new DispatchMatcher([new DriverProfile("driver-1", new GeoPoint(-1.29, 36.82))]);
        var request = new DispatchRequest("request-1", "Central", new GeoPoint(-1.291, 36.821), 5);

        var first = matcher.TryAssign(request);
        var replay = matcher.TryAssign(request);

        Assert.Equal(first, replay);
        Assert.Single(matcher.ListAssignments());
    }

    [Fact]
    public void Reusing_a_request_id_with_a_different_payload_is_a_conflict()
    {
        var matcher = new DispatchMatcher([new DriverProfile("driver-1", new GeoPoint(-1.29, 36.82))]);
        var request = new DispatchRequest("request-1", "Central", new GeoPoint(-1.291, 36.821), 5);
        matcher.TryAssign(request);

        Assert.Throws<DispatchRequestConflictException>(() =>
            matcher.TryAssign(request with { Pickup = new GeoPoint(-1.30, 36.83) }));
    }

    [Fact]
    public void Does_not_assign_a_driver_outside_the_radius()
    {
        var matcher = new DispatchMatcher([new DriverProfile("driver-1", new GeoPoint(-1.30, 36.83))]);

        var assignment = matcher.TryAssign(new DispatchRequest("request-1", "West", new GeoPoint(-1.29, 36.82), 0.1));

        Assert.Null(assignment);
    }
}

public sealed class DriverUpdateLedgerTests
{
    [Fact]
    public void Accepts_exact_replays_and_rejects_reuse_with_different_payload()
    {
        var ledger = new DriverUpdateLedger();
        var update = new DriverUpdate("event-1", "DEMO-0142", "accepted", DateTimeOffset.UtcNow);

        Assert.Equal(UpdateReceiptStatus.Created, ledger.Record(update).Status);
        Assert.Equal(UpdateReceiptStatus.Replay, ledger.Record(update).Status);
        Assert.Equal(
            UpdateReceiptStatus.Conflict,
            ledger.Record(update with { Status = "en-route" }).Status);
        Assert.Single(ledger.ListRecent());
    }
}
