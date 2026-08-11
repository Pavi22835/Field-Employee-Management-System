using FEMS.Application.Admin;
using FEMS.Application.Auth;
using FEMS.Application.Common.Interfaces;
using FEMS.Application.Devices;
using FEMS.Application.DynamicForms;
using FEMS.Application.Employees;
using FEMS.Application.FieldAreas;
using FEMS.Application.FieldAssignments;
using FEMS.Application.FieldVisits;
using FEMS.Application.Roles;
using FEMS.Application.Settings;
using FEMS.Infrastructure.Identity;
using FEMS.Infrastructure.Persistence;
using FEMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FEMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IDateTimeProvider, DateTimeProvider>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IDeviceAdminService, DeviceAdminService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IFieldAreaService, FieldAreaService>();
        services.AddScoped<IFieldAssignmentService, FieldAssignmentService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<ISecurityAlertService, SecurityAlertService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IDynamicFormService, DynamicFormService>();
        services.AddScoped<IFieldVisitService, FieldVisitService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<ISystemSettingsService, SystemSettingsService>();
        services.AddScoped<IPushNotificationService, FcmPushNotificationService>();

        return services;
    }
}
