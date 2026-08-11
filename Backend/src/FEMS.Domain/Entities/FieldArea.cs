using FEMS.Domain.Common;
using FEMS.Domain.Enums;

namespace FEMS.Domain.Entities;

/// <summary>Geofenced area definitions (section 7).</summary>
public class FieldArea : AuditableEntity
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Address { get; set; }

    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }

    /// <summary>Geofence radius in meters.</summary>
    public int RadiusMeters { get; set; }

    public GeofenceEnforcementMode EnforcementMode { get; set; } = GeofenceEnforcementMode.Mandatory;
    public bool IsActive { get; set; } = true;

    public ICollection<FieldAssignment> Assignments { get; set; } = new List<FieldAssignment>();
}
