using FEMS.Domain.Common;
using FEMS.Domain.Enums;

namespace FEMS.Domain.Entities;

/// <summary>
/// Company device inventory and current assignment (section 6 &amp; 17).
/// Identity is derived from an app-scoped GUID + backend registration record,
/// NOT from IMEI (modern Android restricts hardware identifier access).
/// </summary>
public class Device : AuditableEntity
{
    /// <summary>Persistent, app-scoped GUID generated on first launch (encrypted storage on device).</summary>
    public Guid AppInstallationId { get; set; }

    public string? AndroidId { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? OsVersion { get; set; }
    public string? AppVersion { get; set; }

    /// <summary>Reserved for Phase 6 Android Enterprise / MDM enrollment identity.</summary>
    public string? DeviceManagementId { get; set; }

    public string? PushToken { get; set; }

    public DeviceStatus Status { get; set; } = DeviceStatus.Pending;

    public Guid? AssignedEmployeeId { get; set; }
    public Employee? AssignedEmployee { get; set; }

    public DateTimeOffset RegisteredAt { get; set; }

    /// <summary>Reserved: populated once heartbeat infrastructure (Phase 6) is enabled.</summary>
    public DateTimeOffset? LastHeartbeatAt { get; set; }
    public string? LastKnownIp { get; set; }

    public ICollection<DeviceEnrollment> Enrollments { get; set; } = new List<DeviceEnrollment>();
    public ICollection<DeviceCompliance> ComplianceSnapshots { get; set; } = new List<DeviceCompliance>();
    public ICollection<DeviceEvent> Events { get; set; } = new List<DeviceEvent>();
    public ICollection<LoginSession> LoginSessions { get; set; } = new List<LoginSession>();
}
