using BuildingBlocks;

namespace SchoolManager.Attendance.Domain;

/// <summary>A stop of a bus route. Created only through <see cref="BusRoute.SetStops"/>.</summary>
public sealed class BusRouteStop : BaseEntity
{
    public Guid BusRouteId { get; private set; }
    public NameLocalized Name { get; private set; } = default!;

    /// <summary>Position on the route (unique within the route)</summary>
    public int Order { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    private BusRouteStop() { }

    internal static BusRouteStop Create(Guid busRouteId, NameLocalized name, int order, double? latitude, double? longitude)
    {
        if (string.IsNullOrWhiteSpace(name.Arabic) || string.IsNullOrWhiteSpace(name.English))
            throw new DomainException("Stop name is required in Arabic and English.");
        if (order <= 0)
            throw new DomainException("Stop order must be greater than zero.");
        CheckCoordinates(latitude, longitude);

        return new BusRouteStop
        {
            BusRouteId = busRouteId,
            // Copy: NameLocalized is an owned type tracked by reference
            Name = new NameLocalized(name.Arabic.Trim(), name.English.Trim()),
            Order = order,
            Latitude = latitude,
            Longitude = longitude
        };
    }

    /// <summary>Both values or neither, within the valid ranges</summary>
    private static void CheckCoordinates(double? latitude, double? longitude)
    {
        if (latitude.HasValue != longitude.HasValue)
            throw new DomainException("Latitude and longitude must be given together.");
        if (latitude is < -90 or > 90)
            throw new DomainException("Latitude must be between -90 and 90.");
        if (longitude is < -180 or > 180)
            throw new DomainException("Longitude must be between -180 and 180.");
    }
}
