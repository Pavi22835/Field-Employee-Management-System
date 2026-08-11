using FEMS.Domain.Common;

namespace FEMS.Domain.Entities;

/// <summary>
/// Section 21: persisted admin configuration (attendance rules, geofence radius,
/// location tracking mode, session timeout, etc.). Not one of the 21 tables originally
/// enumerated in section 17 — added as an additive extension so the admin configuration
/// screens in the Active Scope (section 3.1) have somewhere to persist values. Modeled
/// as a single-row settings table rather than free-form key/value for type safety.
/// </summary>
public class SystemSetting : AuditableEntity
{
    // Section 15: default org-wide location tracking mode (VisitBased/Periodic/ContinuousDuty).
    public Enums.LocationTrackingMode LocationTrackingMode { get; set; } = Enums.LocationTrackingMode.VisitBased;

    /// <summary>Interval in seconds between location captures while a visit is active and mode is Periodic/ContinuousDuty.</summary>
    public int PeriodicTrackingIntervalSeconds { get; set; } = 300;

    // Section 7/9: default geofence radius (meters) suggested when admins create a new FieldArea.
    public int DefaultGeofenceRadiusMeters { get; set; } = 2000;

    /// <summary>Reserved for Phase 6 heartbeat infrastructure.</summary>
    public int HeartbeatIntervalMinutes { get; set; } = 15;

    public int SessionTimeoutMinutes { get; set; } = 60;
    public int MaxFailedLoginAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;

    public bool DeviceReplacementRequiresApproval { get; set; } = true;
    public string MinimumSupportedAppVersion { get; set; } = "1.0.0";

    public bool NotifyAdminsOnUnregisteredDeviceAttempt { get; set; } = true;
    public bool NotifyAdminsOnDeviceNonCompliance { get; set; } = true;
    public bool NotifyEmployeeOnNewAssignment { get; set; } = true;
}
