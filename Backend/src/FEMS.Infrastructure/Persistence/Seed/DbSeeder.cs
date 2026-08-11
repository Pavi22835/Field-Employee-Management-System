using FEMS.Application.Common.Interfaces;
using FEMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FEMS.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds the four roles from section 4 (Super Admin, Admin, Supervisor, Employee) and,
/// on first run only, a bootstrap Super Admin account so the system is usable after
/// `dotnet ef database update`. The bootstrap password must be changed on first login.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, IPasswordHasher hasher, IConfiguration configuration)
    {
        await db.Database.MigrateAsync();

        var roleNames = new[] { "SuperAdmin", "Admin", "Supervisor", "Employee" };
        foreach (var name in roleNames)
        {
            if (!await db.Roles.AnyAsync(r => r.Name == name))
                db.Roles.Add(new Role { Name = name, IsSystemRole = true, CreatedAt = DateTimeOffset.UtcNow });
        }
        await db.SaveChangesAsync();

        if (!await db.Users.AnyAsync())
        {
            var superAdminRole = await db.Roles.FirstAsync(r => r.Name == "SuperAdmin");
            var bootstrapPassword = configuration["Seed:SuperAdminPassword"] ?? "ChangeMe!123";

            var user = new User
            {
                Username = "superadmin",
                Email = configuration["Seed:SuperAdminEmail"] ?? "admin@example.com",
                PasswordHash = hasher.Hash(bootstrapPassword),
                IsActive = true,
                MustChangePassword = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = superAdminRole.Id, CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }

        // Section 21: a single default settings row is created if none exists yet, so the
        // admin configuration screens always have something to read/update.
        if (!await db.SystemSettings.AnyAsync())
        {
            db.SystemSettings.Add(new SystemSetting { CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }
    }
}
