using FEMS.Application.Common.Interfaces;
using FEMS.Application.Common.Models;
using FEMS.Application.Devices;
using FEMS.Domain.Entities;
using FEMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FEMS.Infrastructure.Services;

/// <summary>
/// Section 6: device registration & binding using app-installation GUID + Android ID,
/// not IMEI. New enrollments start as Pending until an admin approves/activates them
/// (see POST /api/admin/devices/{id}/approve).
/// </summary>
public class DeviceService : IDeviceService
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _dateTime;

    public DeviceService(IApplicationDbContext db, IDateTimeProvider dateTime)
    {
        _db = db;
        _dateTime = dateTime;
    }

    public async Task<DeviceStatusResponse> EnrollAsync(Guid employeeId, EnrollDeviceRequest request, string? ipAddress, CancellationToken ct = default)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, ct)
            ?? throw new NotFoundException(nameof(Employee), employeeId);

        var device = await _db.Devices.FirstOrDefaultAsync(d => d.AppInstallationId == request.AppInstallationId, ct);

        if (device is null)
        {
            device = new Device
            {
                AppInstallationId = request.AppInstallationId,
                RegisteredAt = _dateTime.UtcNow,
                Status = DeviceStatus.Pending
            };
            _db.Devices.Add(device);
        }

        device.AndroidId = request.AndroidId;
        device.Manufacturer = request.Manufacturer;
        device.Model = request.Model;
        device.OsVersion = request.OsVersion;
        device.AppVersion = request.AppVersion;
        device.PushToken = request.PushToken;
        device.AssignedEmployeeId = employee.Id;
        device.LastKnownIp = ipAddress;

        _db.DeviceEnrollments.Add(new DeviceEnrollment
        {
            Device = device,
            EmployeeId = employee.Id,
            EnrolledAt = _dateTime.UtcNow
        });

        await EvaluateComplianceAsync(device, employee, ct);

        await _db.SaveChangesAsync(ct);

        return new DeviceStatusResponse(device.Id, device.Status.ToString(), device.AssignedEmployeeId, device.RegisteredAt, device.Status == DeviceStatus.Active);
    }

    /// <summary>
    /// Section 13.3 &amp; 21: compares the reported app version against the admin-configured
    /// minimum (SystemSettings.MinimumSupportedAppVersion) and records a compliance
    /// snapshot, raising a security alert on non-compliance per section 20.2.
    /// </summary>
    private async Task EvaluateComplianceAsync(Device device, Employee employee, CancellationToken ct)
    {
        var settings = await _db.SystemSettings.FirstOrDefaultAsync(ct);
        var minVersion = settings?.MinimumSupportedAppVersion;

        var isCompliant = true;
        var notes = "App version meets the minimum supported version.";

        if (!string.IsNullOrWhiteSpace(minVersion) && !string.IsNullOrWhiteSpace(device.AppVersion))
        {
            if (Version.TryParse(device.AppVersion, out var reported) && Version.TryParse(minVersion, out var minimum))
            {
                isCompliant = reported >= minimum;
                notes = isCompliant
                    ? "App version meets the minimum supported version."
                    : $"App version {device.AppVersion} is below the minimum supported version {minVersion}.";
            }
        }

        _db.DeviceCompliances.Add(new DeviceCompliance
        {
            DeviceId = device.Id,
            IsCompliant = isCompliant,
            MinAppVersionCheck = minVersion,
            OsVersionCheck = device.OsVersion,
            Notes = notes,
            EvaluatedAt = _dateTime.UtcNow
        });

        if (!isCompliant && (settings?.NotifyAdminsOnDeviceNonCompliance ?? true))
        {
            _db.SecurityAlerts.Add(new SecurityAlert
            {
                EmployeeId = employee.Id,
                DeviceId = device.Id,
                AlertType = "DeviceNonCompliant",
                Severity = SecurityAlertSeverity.Warning,
                Message = $"Device assigned to {employee.FirstName} {employee.LastName} has become non-compliant: {notes}"
            });
        }
    }

    public async Task<DeviceStatusResponse> GetMyDeviceAsync(Guid employeeId, CancellationToken ct = default)
    {
        var device = await _db.Devices
            .Where(d => d.AssignedEmployeeId == employeeId && !d.IsDeleted)
            .OrderByDescending(d => d.RegisteredAt)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Device for employee", employeeId);

        return new DeviceStatusResponse(device.Id, device.Status.ToString(), device.AssignedEmployeeId, device.RegisteredAt, device.Status == DeviceStatus.Active);
    }

    public async Task ReportEventAsync(Guid deviceId, Guid? employeeId, ReportDeviceEventRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<DeviceEventType>(request.EventType, ignoreCase: true, out var eventType))
            throw new AppException($"Unknown device event type '{request.EventType}'.");

        _db.DeviceEvents.Add(new DeviceEvent
        {
            DeviceId = deviceId,
            EmployeeId = employeeId,
            EventType = eventType,
            OccurredAt = request.OccurredAt,
            Metadata = request.Metadata
        });

        await _db.SaveChangesAsync(ct);
    }
}
