using FEMS.Application.Common.Interfaces;
using FEMS.Application.Common.Models;
using FEMS.Application.Devices;
using FEMS.Domain.Entities;
using FEMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FEMS.Infrastructure.Services;

/// <summary>Section 16: admin device management actions.</summary>
public class DeviceAdminService : IDeviceAdminService
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _dateTime;
    private readonly IPushNotificationService _pushNotificationService;

    public DeviceAdminService(IApplicationDbContext db, IDateTimeProvider dateTime, IPushNotificationService pushNotificationService)
    {
        _db = db;
        _dateTime = dateTime;
        _pushNotificationService = pushNotificationService;
    }

    public async Task<PagedResult<DeviceListItemResponse>> GetListAsync(int pageNumber, int pageSize, string? statusFilter, CancellationToken ct = default)
    {
        var query = _db.Devices.Include(d => d.AssignedEmployee).Where(d => !d.IsDeleted).AsQueryable();

        if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<DeviceStatus>(statusFilter, true, out var status))
            query = query.Where(d => d.Status == status);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(d => d.RegisteredAt)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(d => new DeviceListItemResponse(
                d.Id, d.AssignedEmployeeId,
                d.AssignedEmployee != null ? d.AssignedEmployee.FirstName + " " + d.AssignedEmployee.LastName : null,
                d.Model, d.Manufacturer, d.OsVersion, d.Status.ToString(),
                d.Status == DeviceStatus.Active, d.LastHeartbeatAt, d.AppVersion))
            .ToListAsync(ct);

        return new PagedResult<DeviceListItemResponse> { Items = items, PageNumber = pageNumber, PageSize = pageSize, TotalCount = total };
    }

    public async Task ApproveAsync(Guid deviceId, CancellationToken ct = default)
    {
        var device = await GetDeviceAsync(deviceId, ct);
        device.Status = DeviceStatus.Active;
        _db.DeviceCompliances.Add(new DeviceCompliance { DeviceId = device.Id, IsCompliant = true, EvaluatedAt = _dateTime.UtcNow, Notes = "Approved by admin." });
        await _db.SaveChangesAsync(ct);
    }

    public async Task RevokeAsync(Guid deviceId, string? reason, CancellationToken ct = default)
    {
        var device = await GetDeviceAsync(deviceId, ct);
        device.Status = DeviceStatus.Revoked;

        var enrollment = await _db.DeviceEnrollments
            .Where(e => e.DeviceId == device.Id && e.RevokedAt == null)
            .OrderByDescending(e => e.EnrolledAt).FirstOrDefaultAsync(ct);
        if (enrollment is not null)
        {
            enrollment.RevokedAt = _dateTime.UtcNow;
            enrollment.RevokedReason = reason;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task AssignAsync(Guid deviceId, AssignDeviceRequest request, CancellationToken ct = default)
    {
        var device = await GetDeviceAsync(deviceId, ct);
        var employeeExists = await _db.Employees.AnyAsync(e => e.Id == request.EmployeeId && !e.IsDeleted, ct);
        if (!employeeExists) throw new NotFoundException(nameof(Employee), request.EmployeeId);

        device.AssignedEmployeeId = request.EmployeeId;
        await _db.SaveChangesAsync(ct);
    }

    public async Task UnassignAsync(Guid deviceId, CancellationToken ct = default)
    {
        var device = await GetDeviceAsync(deviceId, ct);
        device.AssignedEmployeeId = null;
        device.Status = DeviceStatus.Suspended;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkLostAsync(Guid deviceId, MarkDeviceLostRequest request, CancellationToken ct = default)
    {
        var device = await GetDeviceAsync(deviceId, ct);
        device.Status = DeviceStatus.Lost;

        _db.SecurityAlerts.Add(new SecurityAlert
        {
            DeviceId = device.Id,
            EmployeeId = device.AssignedEmployeeId,
            AlertType = "DeviceLost",
            Severity = SecurityAlertSeverity.Critical,
            Message = request.Notes ?? "Device marked lost by admin.",
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task LockEmployeeAccountAsync(Guid employeeId, CancellationToken ct = default)
    {
        var employee = await _db.Employees.Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Employee), employeeId);

        employee.User.IsLockedOut = true;
        employee.User.LockoutEnd = _dateTime.UtcNow.AddYears(100); // indefinite, admin must unlock explicitly
        await _db.SaveChangesAsync(ct);
    }

    public async Task ForceLogoutAsync(Guid employeeId, CancellationToken ct = default)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Employee), employeeId);

        var activeSessions = await _db.LoginSessions.Where(s => s.UserId == employee.UserId && s.IsActive).ToListAsync(ct);
        foreach (var session in activeSessions)
        {
            session.IsActive = false;
            session.LogoutAt = _dateTime.UtcNow;
        }

        var activeTokens = await _db.RefreshTokens.Where(t => t.UserId == employee.UserId && t.RevokedAt == null).ToListAsync(ct);
        foreach (var token in activeTokens)
            token.RevokedAt = _dateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task SendNotificationAsync(Guid deviceId, SendDeviceNotificationRequest request, CancellationToken ct = default)
    {
        var device = await GetDeviceAsync(deviceId, ct);
        if (device.AssignedEmployeeId is null)
            throw new AppException("Device has no assigned employee to notify.");

        var employee = await _db.Employees.FirstAsync(e => e.Id == device.AssignedEmployeeId, ct);

        var notification = new Notification
        {
            RecipientUserId = employee.UserId,
            Title = request.Title,
            Body = request.Body,
            Channel = NotificationChannel.Push
        };
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(device.PushToken))
        {
            await _pushNotificationService.SendAsync(device.PushToken, request.Title, request.Body, ct: ct);
            notification.SentAt = _dateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task<Device> GetDeviceAsync(Guid id, CancellationToken ct) =>
        await _db.Devices.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, ct)
        ?? throw new NotFoundException(nameof(Device), id);
}
