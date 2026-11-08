namespace DispatchApi.Domain;

public sealed class DispatchMatcher
{
    private const double EarthRadiusKm = 6_371.0088;
    private readonly object _gate = new();
    private readonly Dictionary<string, DriverProfile> _driversById;
    private readonly HashSet<string> _availableDriverIds;
    private readonly Dictionary<string, (DispatchRequest Request, DispatchAssignment Assignment)> _assignmentsByRequestId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DispatchAssignment> _assignmentsById = new(StringComparer.Ordinal);

    public DispatchMatcher(IEnumerable<DriverProfile> drivers)
    {
        ArgumentNullException.ThrowIfNull(drivers);
        _driversById = drivers.ToDictionary(driver => driver.Id, StringComparer.Ordinal);
        _availableDriverIds = _driversById.Keys.ToHashSet(StringComparer.Ordinal);
    }

    public DispatchAssignment? TryAssign(DispatchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.PickupLabel) || request.PickupLabel.Length > 80)
            throw new ArgumentException("A pickup label of 1 to 80 characters is required.", nameof(request));
        request = request with { PickupLabel = request.PickupLabel.Trim() };
        ValidateRequest(request);

        lock (_gate)
        {
            if (_assignmentsByRequestId.TryGetValue(request.Id, out var existing))
            {
                if (existing.Request != request)
                    throw new DispatchRequestConflictException(request.Id);
                return existing.Assignment;
            }

            var candidate = _availableDriverIds
                .Select(id => _driversById[id])
                .Select(driver => new
                {
                    Driver = driver,
                    DistanceKm = DistanceInKilometers(driver.Location, request.Pickup)
                })
                .Where(candidate => candidate.DistanceKm <= request.MaximumDistanceKm)
                .OrderBy(candidate => candidate.DistanceKm)
                .ThenBy(candidate => candidate.Driver.Id, StringComparer.Ordinal)
                .FirstOrDefault();

            if (candidate is null)
                return null;

            var assignment = new DispatchAssignment(
                Guid.NewGuid().ToString("N"),
                request.Id,
                request.PickupLabel,
                candidate.Driver.Id,
                candidate.DistanceKm,
                DateTimeOffset.UtcNow);

            _availableDriverIds.Remove(candidate.Driver.Id);
            _assignmentsByRequestId.Add(request.Id, (request, assignment));
            _assignmentsById.Add(assignment.Id, assignment);
            return assignment;
        }
    }

    public DispatchAssignment? FindAssignment(string id)
    {
        lock (_gate)
            return _assignmentsById.GetValueOrDefault(id);
    }

    public IReadOnlyCollection<DispatchAssignment> ListAssignments()
    {
        lock (_gate)
            return _assignmentsById.Values.OrderByDescending(assignment => assignment.AssignedAt).ToArray();
    }

    private static double DistanceInKilometers(GeoPoint from, GeoPoint to)
    {
        var latitudeDelta = DegreesToRadians(to.Latitude - from.Latitude);
        var longitudeDelta = DegreesToRadians(to.Longitude - from.Longitude);
        var fromLatitude = DegreesToRadians(from.Latitude);
        var toLatitude = DegreesToRadians(to.Latitude);

        var haversine = Math.Pow(Math.Sin(latitudeDelta / 2), 2)
            + Math.Cos(fromLatitude) * Math.Cos(toLatitude) * Math.Pow(Math.Sin(longitudeDelta / 2), 2);

        return EarthRadiusKm * 2 * Math.Asin(Math.Sqrt(Math.Clamp(haversine, 0, 1)));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;

    private static void ValidateRequest(DispatchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
            throw new ArgumentException("A request ID is required.", nameof(request));
        if (!IsValidPoint(request.Pickup))
            throw new ArgumentOutOfRangeException(nameof(request), "Pickup coordinates are invalid.");
        if (!double.IsFinite(request.MaximumDistanceKm) || request.MaximumDistanceKm <= 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Maximum distance must be positive and finite.");
    }

    private static bool IsValidPoint(GeoPoint point) =>
        double.IsFinite(point.Latitude)
        && double.IsFinite(point.Longitude)
        && point.Latitude is >= -90 and <= 90
        && point.Longitude is >= -180 and <= 180;
}
