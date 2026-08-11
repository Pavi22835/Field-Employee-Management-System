using FEMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FEMS.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the EF Core DbContext so the Application layer never
/// references Infrastructure/EF Core directly (Clean Architecture boundary).
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<Employee> Employees { get; }
    DbSet<Device> Devices { get; }
    DbSet<DeviceEnrollment> DeviceEnrollments { get; }
    DbSet<DeviceCompliance> DeviceCompliances { get; }
    DbSet<DeviceEvent> DeviceEvents { get; }
    DbSet<LoginSession> LoginSessions { get; }
    DbSet<Attendance> Attendances { get; }
    DbSet<FieldArea> FieldAreas { get; }
    DbSet<FieldAssignment> FieldAssignments { get; }
    DbSet<FieldVisit> FieldVisits { get; }
    DbSet<FieldVisitLocation> FieldVisitLocations { get; }
    DbSet<DynamicForm> DynamicForms { get; }
    DbSet<FormField> FormFields { get; }
    DbSet<FormSubmission> FormSubmissions { get; }
    DbSet<FormSubmissionFile> FormSubmissionFiles { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<SecurityAlert> SecurityAlerts { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<AppVersion> AppVersions { get; }
    DbSet<SystemSetting> SystemSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
