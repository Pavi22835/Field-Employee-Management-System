using FEMS.Application.Auth;
using FEMS.Application.Common.Interfaces;
using FEMS.Application.Common.Models;
using FEMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FEMS.Infrastructure.Services;

/// <summary>
/// Section 19: login issues a JWT + rotating refresh token; account lockout after
/// configurable failed attempts; device binding validated when the caller supplies
/// a device app-installation id.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _dateTime;
    private readonly IConfiguration _configuration;

    public AuthService(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IDateTimeProvider dateTime,
        IConfiguration configuration)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _dateTime = dateTime;
        _configuration = configuration;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Username == request.Username, ct);

        if (user is null)
            throw new UnauthorizedAppException("Invalid username or password.");

        var maxAttempts = int.Parse(_configuration["Security:MaxFailedLoginAttempts"] ?? "5");
        var lockoutMinutes = int.Parse(_configuration["Security:LockoutMinutes"] ?? "15");

        if (user.IsLockedOut && user.LockoutEnd.HasValue && user.LockoutEnd > _dateTime.UtcNow)
            throw new ForbiddenAppException("Account is locked. Try again later.");

        if (!user.IsActive)
            throw new ForbiddenAppException("Account is inactive.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.AccessFailedCount++;
            if (user.AccessFailedCount >= maxAttempts)
            {
                user.IsLockedOut = true;
                user.LockoutEnd = _dateTime.UtcNow.AddMinutes(lockoutMinutes);
            }
            await _db.SaveChangesAsync(ct);
            throw new UnauthorizedAppException("Invalid username or password.");
        }

        // Device binding: if the client presents a device id, it must be an Active
        // device already assigned to this employee (section 19 - "device binding").
        var deviceVerified = false;
        Guid? deviceId = null;
        if (request.DeviceAppInstallationId.HasValue && user.Employee is not null)
        {
            var device = await _db.Devices.FirstOrDefaultAsync(
                d => d.AppInstallationId == request.DeviceAppInstallationId.Value, ct);

            if (device is not null && device.AssignedEmployeeId == user.Employee.Id &&
                device.Status == Domain.Enums.DeviceStatus.Active)
            {
                deviceVerified = true;
                deviceId = device.Id;
            }
            else
            {
                // Section 20.2: "Employee John attempted login from an unregistered device."
                // Recorded regardless of the NotifyAdminsOnUnregisteredDeviceAttempt setting —
                // that flag (section 21) only gates whether a push notification also fires.
                _db.SecurityAlerts.Add(new SecurityAlert
                {
                    EmployeeId = user.Employee.Id,
                    DeviceId = device?.Id,
                    AlertType = "UnregisteredDeviceLogin",
                    Severity = Domain.Enums.SecurityAlertSeverity.Critical,
                    Message = $"Employee {user.Username} attempted login from an unregistered or inactive device."
                });

                if (device is not null)
                {
                    _db.DeviceEvents.Add(new DeviceEvent
                    {
                        DeviceId = device.Id,
                        EmployeeId = user.Employee.Id,
                        EventType = Domain.Enums.DeviceEventType.UnregisteredDeviceAttempt,
                        OccurredAt = _dateTime.UtcNow
                    });
                }
            }
        }

        user.AccessFailedCount = 0;
        user.IsLockedOut = false;
        user.LockoutEnd = null;
        user.LastLoginAt = _dateTime.UtcNow;

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles, user.Employee?.Id, deviceId);
        var (refreshToken, expiresAt) = _jwtTokenService.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _jwtTokenService.HashToken(refreshToken),
            ExpiresAt = expiresAt,
            CreatedByIp = ipAddress
        });

        _db.LoginSessions.Add(new LoginSession
        {
            UserId = user.Id,
            DeviceId = deviceId,
            LoginAt = _dateTime.UtcNow,
            IpAddress = ipAddress,
            IsActive = true
        });

        await _db.SaveChangesAsync(ct);

        var jwtSection = _configuration.GetSection("Jwt");
        var accessMinutes = int.Parse(jwtSection["AccessTokenExpiryMinutes"] ?? "15");

        return new LoginResponse(
            accessToken,
            refreshToken,
            _dateTime.UtcNow.AddMinutes(accessMinutes),
            user.Id,
            user.Username,
            roles,
            user.Employee?.Id,
            deviceVerified);
    }

    public async Task<LoginResponse> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken ct = default)
    {
        var hash = _jwtTokenService.HashToken(request.RefreshToken);

        var existing = await _db.RefreshTokens
            .Include(rt => rt.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(rt => rt.User).ThenInclude(u => u.Employee)
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash, ct);

        if (existing is null)
            throw new UnauthorizedAppException("Invalid refresh token.");

        if (!existing.IsActive)
        {
            // Reuse of a revoked/expired token: revoke the whole chain for this user (reuse detection).
            var allActive = _db.RefreshTokens.Where(rt => rt.UserId == existing.UserId && rt.RevokedAt == null);
            await foreach (var rt in allActive.AsAsyncEnumerable().WithCancellation(ct))
                rt.RevokedAt = _dateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            throw new UnauthorizedAppException("Refresh token has been revoked. Please log in again.");
        }

        var (newToken, newExpiresAt) = _jwtTokenService.GenerateRefreshToken();
        var newHash = _jwtTokenService.HashToken(newToken);

        existing.RevokedAt = _dateTime.UtcNow;
        existing.RevokedByIp = ipAddress;
        existing.ReplacedByTokenHash = newHash;

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = existing.UserId,
            TokenHash = newHash,
            ExpiresAt = newExpiresAt,
            CreatedByIp = ipAddress
        });

        var user = existing.User;
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles, user.Employee?.Id, null);

        await _db.SaveChangesAsync(ct);

        var jwtSection = _configuration.GetSection("Jwt");
        var accessMinutes = int.Parse(jwtSection["AccessTokenExpiryMinutes"] ?? "15");

        return new LoginResponse(
            accessToken, newToken, _dateTime.UtcNow.AddMinutes(accessMinutes),
            user.Id, user.Username, roles, user.Employee?.Id, false);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken ct = default)
    {
        var hash = _jwtTokenService.HashToken(request.RefreshToken);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash, ct);
        if (existing is not null && existing.RevokedAt is null)
        {
            existing.RevokedAt = _dateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }
}
