using BuildingBlocks;

namespace SchoolManager.Attendance.Domain;

/// <summary>A bus route (خط السير) with its stops in order</summary>
public sealed class BusRoute : BaseEntity
{
    public const int MaxDescriptionLength = 500;

    public NameLocalized Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public AuditInfo AuditInfo { get; private set; } = default!;

    private readonly List<BusRouteStop> _stops = new();
    public IReadOnlyList<BusRouteStop> Stops => _stops;

    private BusRoute() { }

    public static BusRoute Create(NameLocalized name, string? description, AuditInfo auditInfo)
    {
        var route = new BusRoute { IsActive = true };
        route.Update(name, description, auditInfo);
        return route;
    }

    public void Update(NameLocalized name, string? description, AuditInfo auditInfo)
    {
        if (string.IsNullOrWhiteSpace(name.Arabic) || string.IsNullOrWhiteSpace(name.English))
            throw new DomainException("Route name is required in Arabic and English.");

        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (description?.Length > MaxDescriptionLength)
            throw new DomainException($"Description cannot exceed {MaxDescriptionLength} characters.");

        Name = new NameLocalized(name.Arabic.Trim(), name.English.Trim());
        Description = description;
        AuditInfo = auditInfo;
    }

    /// <summary>
    /// Replaces the route's stops. Each stop's order must be unique (1, 2, 3 … the order the bus passes them).
    /// </summary>
    public void SetStops(IEnumerable<(NameLocalized Name, int Order)> stops)
    {
        var list = stops.ToList();
        if (list.GroupBy(s => s.Order).Any(g => g.Count() > 1))
            throw new DomainException("Stop order must be unique.");

        _stops.Clear();
        foreach (var (name, order) in list.OrderBy(s => s.Order))
            _stops.Add(BusRouteStop.Create(Id, name, order));
    }

    public void Activate(AuditInfo auditInfo)
    {
        IsActive = true;
        AuditInfo = auditInfo;
    }

    public void Deactivate(AuditInfo auditInfo)
    {
        IsActive = false;
        AuditInfo = auditInfo;
    }
}
