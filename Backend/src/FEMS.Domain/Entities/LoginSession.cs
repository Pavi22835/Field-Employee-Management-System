using FEMS.Domain.Common;

namespace FEMS.Domain.Entities;

/// <summary>Active/historical session tracking (section 17).</summary>
public class LoginSession : AuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    public Guid? DeviceId { get; set; }
    public Device? Device { get; set; }

    public DateTimeOffset LoginAt { get; set; }
    public DateTimeOffset? LogoutAt { get; set; }
    public string? IpAddress { get; set; }
    public bool IsActive { get; set; } = true;
}
