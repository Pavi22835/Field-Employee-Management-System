using FEMS.Application.Common.Interfaces;
using FEMS.Domain.Common;
using FEMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FEMS.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService? _currentUser;
    private readonly IDateTimeProvider? _dateTime;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService? currentUser = null,
        IDateTimeProvider? dateTime = null) : base(options)
    {
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<DeviceEnrollment> DeviceEnrollments => Set<DeviceEnrollment>();
    public DbSet<DeviceCompliance> DeviceCompliances => Set<DeviceCompliance>();
    public DbSet<DeviceEvent> DeviceEvents => Set<DeviceEvent>();
    public DbSet<LoginSession> LoginSessions => Set<LoginSession>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<FieldArea> FieldAreas => Set<FieldArea>();
    public DbSet<FieldAssignment> FieldAssignments => Set<FieldAssignment>();
    public DbSet<FieldVisit> FieldVisits => Set<FieldVisit>();
    public DbSet<FieldVisitLocation> FieldVisitLocations => Set<FieldVisitLocation>();
    public DbSet<DynamicForm> DynamicForms => Set<DynamicForm>();
    public DbSet<FormField> FormFields => Set<FormField>();
    public DbSet<FormSubmission> FormSubmissions => Set<FormSubmission>();
    public DbSet<FormSubmissionFile> FormSubmissionFiles => Set<FormSubmissionFile>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SecurityAlert> SecurityAlerts => Set<SecurityAlert>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AppVersion> AppVersions => Set<AppVersion>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global soft-delete query filter for every AuditableEntity-derived type.
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(ApplicationDbContext)
                    .GetMethod(nameof(SetSoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);
                method.Invoke(null, new object[] { builder });
            }
        }

        base.OnModelCreating(builder);
    }

    private static void SetSoftDeleteFilter<TEntity>(ModelBuilder builder) where TEntity : AuditableEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = _dateTime?.UtcNow ?? DateTimeOffset.UtcNow;
        var userId = _currentUser?.UserId;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
                case EntityState.Deleted:
                    // Prefer soft delete: convert hard deletes to an update.
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = now;
                    entry.Entity.DeletedBy = userId;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
