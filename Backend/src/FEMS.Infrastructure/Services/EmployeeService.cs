using FEMS.Application.Common.Interfaces;
using FEMS.Application.Common.Models;
using FEMS.Application.Employees;
using FEMS.Domain.Entities;
using FEMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FEMS.Infrastructure.Services;

/// <summary>
/// Section 4 &amp; 17: creating an employee also provisions the linked User account.
/// The "Add Employee" flow provisions the Employee or Supervisor role — a Supervisor is
/// still an Employee record in this domain model (it needs an EmployeeId for the
/// Employee.SupervisorId scoping used throughout field-visit/employee queries), so it
/// reuses this same endpoint rather than a separate one. Admin/SuperAdmin accounts have
/// no Employee record at all and are provisioned via the SuperAdmin-only UsersController
/// instead (see section 4's RBAC requirement: Admin manages employees/supervisors, but
/// cannot mint more Admins from this form).
/// </summary>
public class EmployeeService : IEmployeeService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTime;
    private readonly ICurrentUserService _currentUser;

    private static readonly string[] AssignableRoles = { "Employee", "Supervisor" };

    public EmployeeService(IApplicationDbContext db, IPasswordHasher passwordHasher, IDateTimeProvider dateTime, ICurrentUserService currentUser)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _dateTime = dateTime;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<EmployeeResponse>> GetListAsync(int pageNumber, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = _db.Employees.Include(e => e.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(e => e.Devices)
            .Where(e => !e.IsDeleted)
            .AsQueryable();

        // Section 4: Supervisor/Manager reads only their own directly-assigned employees.
        var isUnrestricted = _currentUser.IsInRole("SuperAdmin") || _currentUser.IsInRole("Admin");
        if (!isUnrestricted && _currentUser.IsInRole("Supervisor"))
        {
            var supervisorId = _currentUser.EmployeeId;
            query = query.Where(e => e.SupervisorId == supervisorId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(e =>
                e.FirstName.ToLower().Contains(term) ||
                e.LastName.ToLower().Contains(term) ||
                e.EmployeeCode.ToLower().Contains(term) ||
                e.User.Email.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(e => ToResponse(e))
            .ToListAsync(ct);

        return new PagedResult<EmployeeResponse> { Items = items, PageNumber = pageNumber, PageSize = pageSize, TotalCount = total };
    }

    public async Task<EmployeeResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var employee = await _db.Employees.Include(e => e.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(e => e.Devices)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Employee), id);

        return ToResponse(employee);
    }

    public async Task<EmployeeResponse> CreateAsync(CreateEmployeeRequest request, CancellationToken ct = default)
    {
        if (!AssignableRoles.Contains(request.Role))
            throw new AppException($"Role must be one of: {string.Join(", ", AssignableRoles)}.");

        if (await _db.Users.AnyAsync(u => u.Username == request.Username, ct))
            throw new AppException("Username is already taken.", 409);
        if (await _db.Users.AnyAsync(u => u.Email == request.Email, ct))
            throw new AppException("Email is already in use.", 409);
        if (await _db.Employees.AnyAsync(e => e.EmployeeCode == request.EmployeeCode, ct))
            throw new AppException("Employee code is already in use.", 409);

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == request.Role, ct)
            ?? throw new AppException($"Role '{request.Role}' is not configured.");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.TemporaryPassword),
            IsActive = true,
            MustChangePassword = true,
            CreatedAt = _dateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, CreatedAt = _dateTime.UtcNow });

        var employee = new Employee
        {
            UserId = user.Id,
            EmployeeCode = request.EmployeeCode,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            Designation = request.Designation,
            Department = request.Department,
            SupervisorId = request.SupervisorId,
            DateOfJoining = request.DateOfJoining,
            IsActive = true,
            CreatedAt = _dateTime.UtcNow
        };
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(employee.Id, ct);
    }

    public async Task<EmployeeResponse> UpdateAsync(Guid id, UpdateEmployeeRequest request, CancellationToken ct = default)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Employee), id);

        employee.FirstName = request.FirstName;
        employee.LastName = request.LastName;
        employee.PhoneNumber = request.PhoneNumber;
        employee.Designation = request.Designation;
        employee.Department = request.Department;
        employee.SupervisorId = request.SupervisorId;
        employee.IsActive = request.IsActive;

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var employee = await _db.Employees.Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Employee), id);

        employee.IsActive = false;
        employee.IsDeleted = true;
        employee.DeletedAt = _dateTime.UtcNow;
        employee.User.IsActive = false;

        await _db.SaveChangesAsync(ct);
    }

    private static EmployeeResponse ToResponse(Employee e) => new(
        e.Id, e.EmployeeCode, e.FirstName, e.LastName, e.User.Email, e.User.Username,
        e.PhoneNumber, e.Designation, e.Department, e.SupervisorId, e.IsActive, e.DateOfJoining,
        e.User.UserRoles.Select(ur => ur.Role.Name).ToList(),
        e.Devices.Any(d => !d.IsDeleted && d.Status == DeviceStatus.Active));
}
