using FEMS.Application.Common.Interfaces;
using FEMS.Application.Common.Models;
using FEMS.Application.Users;
using FEMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FEMS.Infrastructure.Services;

/// <summary>
/// Section 4: management of pure system accounts (Admin/SuperAdmin) — these have no
/// linked Employee record, unlike Employee/Supervisor accounts (see EmployeeService,
/// which provisions those two roles since a Supervisor still needs an EmployeeId for
/// the Employee.SupervisorId scoping used elsewhere). Every action here is gated
/// SuperAdmin-only at the controller (PolicyNames.SuperAdminOnly) so an Admin can never
/// mint itself — or anyone else — another Admin or SuperAdmin account.
/// </summary>
public class UserManagementService : IUserManagementService
{
    private static readonly string[] AssignableRoles = { "Admin", "SuperAdmin" };

    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTime;

    public UserManagementService(IApplicationDbContext db, IPasswordHasher passwordHasher, IDateTimeProvider dateTime)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _dateTime = dateTime;
    }

    public async Task<IReadOnlyList<SystemUserResponse>> GetListAsync(CancellationToken ct = default) =>
        await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Where(u => !u.IsDeleted && u.Employee == null &&
                        u.UserRoles.Any(ur => !ur.IsDeleted && (ur.Role.Name == "Admin" || ur.Role.Name == "SuperAdmin")))
            .OrderBy(u => u.Username)
            .Select(u => new SystemUserResponse(
                u.Id, u.Username, u.Email, u.IsActive, u.LastLoginAt,
                u.UserRoles.Where(ur => !ur.IsDeleted).Select(ur => ur.Role.Name).ToList()))
            .ToListAsync(ct);

    public async Task<SystemUserResponse> CreateAsync(CreateSystemUserRequest request, CancellationToken ct = default)
    {
        if (!AssignableRoles.Contains(request.Role))
            throw new AppException($"Role must be one of: {string.Join(", ", AssignableRoles)}.");

        if (await _db.Users.AnyAsync(u => u.Username == request.Username, ct))
            throw new AppException("Username is already taken.", 409);
        if (await _db.Users.AnyAsync(u => u.Email == request.Email, ct))
            throw new AppException("Email is already in use.", 409);

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
        await _db.SaveChangesAsync(ct);

        return new SystemUserResponse(user.Id, user.Username, user.Email, user.IsActive, user.LastLoginAt, new[] { role.Name });
    }

    public async Task DeactivateAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(User), userId);

        if (!user.UserRoles.Any(ur => !ur.IsDeleted && (ur.Role.Name == "Admin" || ur.Role.Name == "SuperAdmin")))
            throw new AppException("This account is not a system (Admin/SuperAdmin) account.");

        user.IsActive = false;
        await _db.SaveChangesAsync(ct);
    }
}
