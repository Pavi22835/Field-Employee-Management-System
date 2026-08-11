namespace FEMS.Application.Settings;

// Section 21: admin-configurable system settings.
public record SystemSettingsResponse(
    string LocationTrackingMode,
    int PeriodicTrackingIntervalSeconds,
    int DefaultGeofenceRadiusMeters,
    int HeartbeatIntervalMinutes,
    int SessionTimeoutMinutes,
    int MaxFailedLoginAttempts,
    int LockoutMinutes,
    bool DeviceReplacementRequiresApproval,
    string MinimumSupportedAppVersion,
    bool NotifyAdminsOnUnregisteredDeviceAttempt,
    bool NotifyAdminsOnDeviceNonCompliance,
    bool NotifyEmployeeOnNewAssignment);

// Section 15: minimal, non-sensitive subset exposed to the employee mobile app (the
// full SystemSettingsResponse includes lockout/session policy that shouldn't ship to devices).
public record LocationTrackingPolicyResponse(string LocationTrackingMode, int PeriodicTrackingIntervalSeconds);

public record UpdateSystemSettingsRequest(
    string LocationTrackingMode,
    int PeriodicTrackingIntervalSeconds,
    int DefaultGeofenceRadiusMeters,
    int HeartbeatIntervalMinutes,
    int SessionTimeoutMinutes,
    int MaxFailedLoginAttempts,
    int LockoutMinutes,
    bool DeviceReplacementRequiresApproval,
    string MinimumSupportedAppVersion,
    bool NotifyAdminsOnUnregisteredDeviceAttempt,
    bool NotifyAdminsOnDeviceNonCompliance,
    bool NotifyEmployeeOnNewAssignment);
