using FEMS.Application.Common.Interfaces;
using FEMS.Application.Common.Models;
using FEMS.Application.FieldAssignments;
using FEMS.Domain.Entities;
using FEMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FEMS.Infrastructure.Services;

/// <summary>Section 8: field visit assignment and status workflow.</summary>
public class FieldAssignmentService : IFieldAssignmentService
{
    private readonly IApplicationDbContext _db;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly IDateTimeProvider _dateTime;
    private readonly ICurrentUserService _currentUser;

    public FieldAssignmentService(
        IApplicationDbContext db, IPushNotificationService pushNotificationService,
        IDateTimeProvider dateTime, ICurrentUserService currentUser)
    {
        _db = db;
        _pushNotificationService = pushNotificationService;
        _dateTime = dateTime;
        _currentUser = currentUser;
    }

    /// <summary>True for SuperAdmin/Admin, who see and manage every assignment.</summary>
    private bool IsUnrestrictedManagement => _currentUser.IsInRole("SuperAdmin") || _currentUser.IsInRole("Admin");

    public async Task<PagedResult<FieldAssignmentResponse>> GetListAsync(int pageNumber, int pageSize, Guid? employeeId, DateOnly? date, CancellationToken ct = default)
    {
        // IgnoreQueryFilters() + an explicit !a.IsDeleted below: EF Core's global soft-delete
        // filter on Employee would otherwise cascade through Include() and silently drop an
        // assignment from this list the moment its Employee is deactivated — defeating the
        // whole point of soft-delete, which is to keep historical/audit records visible.
        var query = _db.FieldAssignments.IgnoreQueryFilters()
            .Include(a => a.Employee).Include(a => a.FieldArea)
            .Where(a => !a.IsDeleted).AsQueryable();

        // Section 4: Supervisor/Manager is scoped to "assigned employees" only — Admin/SuperAdmin see everyone.
        if (!IsUnrestrictedManagement && _currentUser.IsInRole("Supervisor"))
        {
            var supervisorId = _currentUser.EmployeeId;
            query = query.Where(a => a.Employee.SupervisorId == supervisorId);
        }

        if (employeeId.HasValue) query = query.Where(a => a.EmployeeId == employeeId.Value);
        if (date.HasValue) query = query.Where(a => a.VisitDate == date.Value);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(a => a.VisitDate).ThenBy(a => a.StartTime)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(a => ToResponse(a)).ToListAsync(ct);

        return new PagedResult<FieldAssignmentResponse> { Items = items, PageNumber = pageNumber, PageSize = pageSize, TotalCount = total };
    }

    public async Task<IReadOnlyList<FieldAssignmentResponse>> GetMyVisitsAsync(Guid employeeId, DateOnly date, CancellationToken ct = default) =>
        await _db.FieldAssignments.Include(a => a.Employee).Include(a => a.FieldArea)
            .Where(a => !a.IsDeleted && a.EmployeeId == employeeId && a.VisitDate == date)
            .OrderBy(a => a.StartTime)
            .Select(a => ToResponse(a))
            .ToListAsync(ct);

    public async Task<FieldAssignmentResponse> CreateAsync(CreateFieldAssignmentRequest request, CancellationToken ct = default)
    {
        var employee = await _db.Employees.AnyAsync(e => e.Id == request.EmployeeId && !e.IsDeleted, ct);
        if (!employee) throw new NotFoundException(nameof(Employee), request.EmployeeId);

        var area = await _db.FieldAreas.AnyAsync(a => a.Id == request.FieldAreaId && !a.IsDeleted, ct);
        if (!area) throw new NotFoundException(nameof(FieldArea), request.FieldAreaId);

        var assignment = new FieldAssignment
        {
            EmployeeId = request.EmployeeId,
            FieldAreaId = request.FieldAreaId,
            VisitDate = request.VisitDate,
            StartTime = request.StartTime,
            ExpectedEndTime = request.ExpectedEndTime,
            Priority = request.Priority,
            Instructions = request.Instructions,
            RequiredInformation = request.RequiredInformation,
            DynamicFormId = request.DynamicFormId,
            Status = VisitStatus.Assigned
        };
        _db.FieldAssignments.Add(assignment);
        await _db.SaveChangesAsync(ct);

        var saved = await _db.FieldAssignments.Include(a => a.Employee).Include(a => a.FieldArea)
            .FirstAsync(a => a.Id == assignment.Id, ct);

        await NotifyNewAssignmentAsync(saved, ct);

        return ToResponse(saved);
    }

    /// <summary>Section 20.1: "New field assignment has been created."</summary>
    private async Task NotifyNewAssignmentAsync(FieldAssignment assignment, CancellationToken ct)
    {
        var settings = await _db.SystemSettings.FirstOrDefaultAsync(ct);
        if (settings is not null && !settings.NotifyEmployeeOnNewAssignment) return;

        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == assignment.EmployeeId, ct);
        if (employee is null) return;

        var title = "New Field Assignment";
        var body = $"You have a new visit to {assignment.FieldArea.Name} on {assignment.VisitDate:d}.";

        var notification = new Notification
        {
            RecipientUserId = employee.UserId,
            Title = title,
            Body = body,
            Channel = NotificationChannel.Push
        };
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(ct);

        var device = await _db.Devices
            .Where(d => d.AssignedEmployeeId == employee.Id && !d.IsDeleted && d.PushToken != null)
            .OrderByDescending(d => d.RegisteredAt)
            .FirstOrDefaultAsync(ct);

        if (device?.PushToken is not null)
        {
            await _pushNotificationService.SendAsync(device.PushToken, title, body, ct: ct);
            notification.SentAt = _dateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<FieldAssignmentResponse> UpdateStatusAsync(Guid id, UpdateFieldAssignmentStatusRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<VisitStatus>(request.Status, true, out var status))
            throw new AppException("Invalid status value.");

        var assignment = await _db.FieldAssignments.IgnoreQueryFilters() // see comment in GetListAsync
            .Include(a => a.Employee).Include(a => a.FieldArea)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(FieldAssignment), id);

        var isManagement = IsUnrestrictedManagement || _currentUser.IsInRole("Supervisor");

        if (!isManagement)
        {
            // Employees may only acknowledge their own newly-assigned visit. Every other
            // transition (Started/InProgress/Completed) is driven by the dedicated
            // check-in/submit/complete endpoints, which independently verify ownership;
            // cancelling an assignment is a management-only action.
            if (_currentUser.EmployeeId != assignment.EmployeeId)
                throw new ForbiddenAppException("You can only update your own assignments.");
            if (status != VisitStatus.Accepted || assignment.Status != VisitStatus.Assigned)
                throw new ForbiddenAppException("Employees may only accept a newly assigned visit.");
        }
        else if (!IsUnrestrictedManagement && assignment.Employee.SupervisorId != _currentUser.EmployeeId)
        {
            // Section 4: Supervisor is scoped to their own directly-assigned employees.
            throw new ForbiddenAppException("You can only manage assignments for your own team.");
        }

        assignment.Status = status;
        await _db.SaveChangesAsync(ct);
        return ToResponse(assignment);
    }

    private static FieldAssignmentResponse ToResponse(FieldAssignment a) => new(
        a.Id, a.EmployeeId, $"{a.Employee.FirstName} {a.Employee.LastName}", a.FieldAreaId, a.FieldArea.Name,
        a.FieldArea.Latitude, a.FieldArea.Longitude, a.FieldArea.RadiusMeters,
        a.VisitDate, a.StartTime, a.ExpectedEndTime, a.Priority, a.Instructions, a.DynamicFormId, a.Status.ToString());
}
