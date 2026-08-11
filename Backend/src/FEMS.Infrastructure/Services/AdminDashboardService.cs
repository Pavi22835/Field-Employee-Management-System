using FEMS.Application.Admin;
using FEMS.Application.Common.Interfaces;
using FEMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FEMS.Infrastructure.Services;

/// <summary>Section 13.1-13.3: aggregated dashboard statistics.</summary>
public class AdminDashboardService : IAdminDashboardService
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _dateTime;

    public AdminDashboardService(IApplicationDbContext db, IDateTimeProvider dateTime)
    {
        _db = db;
        _dateTime = dateTime;
    }

    public async Task<DashboardResponse> GetDashboardAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(_dateTime.UtcNow.UtcDateTime);

        var totalEmployees = await _db.Employees.CountAsync(e => !e.IsDeleted, ct);
        var activeEmployees = await _db.Employees.CountAsync(e => !e.IsDeleted && e.IsActive, ct);

        var loggedInUserIds = await _db.LoginSessions
            .Where(s => s.IsActive)
            .Select(s => s.UserId).Distinct().CountAsync(ct);

        var onVisit = await _db.FieldVisits.CountAsync(v => !v.IsDeleted &&
            (v.Status == VisitStatus.Started || v.Status == VisitStatus.InProgress), ct);

        var unresolvedAlerts = await _db.SecurityAlerts.CountAsync(a => !a.IsDeleted && !a.IsAcknowledged, ct);

        var employeeStats = new EmployeeStats(
            totalEmployees, activeEmployees, loggedInUserIds,
            Math.Max(0, activeEmployees - loggedInUserIds),
            onVisit,
            Math.Max(0, activeEmployees - loggedInUserIds - onVisit),
            unresolvedAlerts);

        var todaysAssignments = _db.FieldAssignments.Where(a => !a.IsDeleted && a.VisitDate == today);
        var visitStats = new VisitStats(
            await todaysAssignments.CountAsync(ct),
            await todaysAssignments.CountAsync(a => a.Status == VisitStatus.Completed, ct),
            await todaysAssignments.CountAsync(a => a.Status == VisitStatus.InProgress || a.Status == VisitStatus.Started, ct),
            await todaysAssignments.CountAsync(a => a.Status == VisitStatus.Assigned || a.Status == VisitStatus.Accepted, ct),
            await todaysAssignments.CountAsync(a => a.Status == VisitStatus.Missed, ct),
            await todaysAssignments.CountAsync(a => a.Status == VisitStatus.Cancelled, ct));

        var devices = _db.Devices.Where(d => !d.IsDeleted);
        var deviceStats = new DeviceStats(
            await devices.CountAsync(ct),
            await devices.CountAsync(d => d.Status == DeviceStatus.Active, ct),
            await devices.CountAsync(d => d.Status == DeviceStatus.Suspended || d.Status == DeviceStatus.Pending, ct),
            await devices.CountAsync(d => d.Status == DeviceStatus.Revoked || d.Status == DeviceStatus.Lost, ct),
            await _db.DeviceEvents.CountAsync(e => e.EventType == DeviceEventType.UnregisteredDeviceAttempt, ct),
            0); // SIM/network alerts reserved for a future phase (section 13.3)

        return new DashboardResponse(employeeStats, visitStats, deviceStats);
    }
}
