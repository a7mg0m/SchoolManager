using BuildingBlocks;

namespace SchoolManager.Attendance.Domain;

/// <summary>A stop of a bus route. Created only through <see cref="BusRoute.SetStops"/>.</summary>
public sealed class BusRouteStop : BaseEntity
{
    public Guid BusRouteId { get; private set; }
    public NameLocalized Name { get; private set; } = default!;

    /// <summary>Position on the route (unique within the route)</summary>
    public int Order { get; private set; }

    private BusRouteStop() { }

    internal static BusRouteStop Create(Guid busRouteId, NameLocalized name, int order)
    {
        if (string.IsNullOrWhiteSpace(name.Arabic) || string.IsNullOrWhiteSpace(name.English))
            throw new DomainException("Stop name is required in Arabic and English.");
        if (order <= 0)
            throw new DomainException("Stop order must be greater than zero.");

        return new BusRouteStop
        {
            BusRouteId = busRouteId,
            // Copy: NameLocalized is an owned type tracked by reference
            Name = new NameLocalized(name.Arabic.Trim(), name.English.Trim()),
            Order = order
        };
    }
}
