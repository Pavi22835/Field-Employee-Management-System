using FEMS.Domain.Common;

namespace FEMS.Domain.Entities;

/// <summary>Login credentials, identity, security fields (section 17).</summary>
public class User : AuditableEntity
{
    public string Username { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;

    public bool IsActive { get; set; } = true;
    public bool IsLockedOut { get; set; }
    public int AccessFailedCount { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }

    public bool MustChangePassword { get; set; }
    public DateTimeOffset? PasswordChangedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public Employee? Employee { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<LoginSession> LoginSessions { get; set; } = new List<LoginSession>();
}
