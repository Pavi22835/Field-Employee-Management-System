using FEMS.Application.Admin;
using FEMS.Application.Common.Interfaces;
using FEMS.Application.Common.Models;
using FEMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FEMS.Infrastructure.Services;

/// <summary>Section 13.3, 16, 20.2: list/acknowledge security alerts.</summary>
public class SecurityAlertService : ISecurityAlertService
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _dateTime;

    public SecurityAlertService(IApplicationDbContext db, IDateTimeProvider dateTime)
    {
        _db = db;
        _dateTime = dateTime;
    }

    public async Task<PagedResult<SecurityAlertResponse>> GetListAsync(int pageNumber, int pageSize, bool? unacknowledgedOnly, CancellationToken ct = default)
    {
        var query = _db.SecurityAlerts.Include(a => a.Employee).Where(a => !a.IsDeleted).AsQueryable();
        if (unacknowledgedOnly == true) query = query.Where(a => !a.IsAcknowledged);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(a => a.CreatedAt)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(a => new SecurityAlertResponse(
                a.Id, a.AlertType, a.Severity.ToString(), a.Message,
                a.EmployeeId, a.Employee != null ? a.Employee.FirstName + " " + a.Employee.LastName : null,
                a.DeviceId, a.FieldVisitId, a.IsAcknowledged, a.AcknowledgedAt, a.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<SecurityAlertResponse> { Items = items, PageNumber = pageNumber, PageSize = pageSize, TotalCount = total };
    }

    public async Task AcknowledgeAsync(Guid alertId, Guid acknowledgedByUserId, CancellationToken ct = default)
    {
        var alert = await _db.SecurityAlerts.FirstOrDefaultAsync(a => a.Id == alertId && !a.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(SecurityAlert), alertId);

        alert.IsAcknowledged = true;
        alert.AcknowledgedAt = _dateTime.UtcNow;
        alert.AcknowledgedBy = acknowledgedByUserId;

        await _db.SaveChangesAsync(ct);
    }
}
