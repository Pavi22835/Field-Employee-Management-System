using FEMS.Application.Common.Interfaces;
using FEMS.Application.Common.Models;
using FEMS.Application.Settings;
using FEMS.Domain.Entities;
using FEMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FEMS.Infrastructure.Services;

/// <summary>Section 21: admin configuration, backed by the single-row SystemSettings table.</summary>
public class SystemSettingsService : ISystemSettingsService
{
    private readonly IApplicationDbContext _db;
    public SystemSettingsService(IApplicationDbContext db) => _db = db;

    public async Task<SystemSettingsResponse> GetAsync(CancellationToken ct = default) =>
        ToResponse(await GetOrCreateAsync(ct));

    public async Task<SystemSettingsResponse> UpdateAsync(UpdateSystemSettingsRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<LocationTrackingMode>(request.LocationTrackingMode, true, out var mode))
            throw new AppException("LocationTrackingMode must be one of: VisitBased, Periodic, ContinuousDuty.");

        var settings = await GetOrCreateAsync(ct);

        settings.LocationTrackingMode = mode;
        settings.PeriodicTrackingIntervalSeconds = request.PeriodicTrackingIntervalSeconds;
        settings.DefaultGeofenceRadiusMeters = request.DefaultGeofenceRadiusMeters;
        settings.HeartbeatIntervalMinutes = request.HeartbeatIntervalMinutes;
        settings.SessionTimeoutMinutes = request.SessionTimeoutMinutes;
        settings.MaxFailedLoginAttempts = request.MaxFailedLoginAttempts;
        settings.LockoutMinutes = request.LockoutMinutes;
        settings.DeviceReplacementRequiresApproval = request.DeviceReplacementRequiresApproval;
        settings.MinimumSupportedAppVersion = request.MinimumSupportedAppVersion;
        settings.NotifyAdminsOnUnregisteredDeviceAttempt = request.NotifyAdminsOnUnregisteredDeviceAttempt;
        settings.NotifyAdminsOnDeviceNonCompliance = request.NotifyAdminsOnDeviceNonCompliance;
        settings.NotifyEmployeeOnNewAssignment = request.NotifyEmployeeOnNewAssignment;

        await _db.SaveChangesAsync(ct);
        return ToResponse(settings);
    }

    public async Task<LocationTrackingPolicyResponse> GetTrackingPolicyAsync(CancellationToken ct = default)
    {
        var settings = await GetOrCreateAsync(ct);
        return new LocationTrackingPolicyResponse(settings.LocationTrackingMode.ToString(), settings.PeriodicTrackingIntervalSeconds);
    }

    private async Task<SystemSetting> GetOrCreateAsync(CancellationToken ct)
    {
        var settings = await _db.SystemSettings.FirstOrDefaultAsync(ct);
        if (settings is not null) return settings;

        settings = new SystemSetting();
        _db.SystemSettings.Add(settings);
        await _db.SaveChangesAsync(ct);
        return settings;
    }

    private static SystemSettingsResponse ToResponse(SystemSetting s) => new(
        s.LocationTrackingMode.ToString(), s.PeriodicTrackingIntervalSeconds, s.DefaultGeofenceRadiusMeters,
        s.HeartbeatIntervalMinutes, s.SessionTimeoutMinutes, s.MaxFailedLoginAttempts, s.LockoutMinutes,
        s.DeviceReplacementRequiresApproval, s.MinimumSupportedAppVersion,
        s.NotifyAdminsOnUnregisteredDeviceAttempt, s.NotifyAdminsOnDeviceNonCompliance, s.NotifyEmployeeOnNewAssignment);
}
