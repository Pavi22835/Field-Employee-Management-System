using FEMS.Domain.Common;

namespace FEMS.Domain.Entities;

/// <summary>History of device enrollment events per employee (section 17).</summary>
public class DeviceEnrollment : AuditableEntity
{
    public Guid DeviceId { get; set; }
    public Device Device { get; set; } = default!;

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;

    public DateTimeOffset EnrolledAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }

    /// <summary>Play Integrity API (or equivalent) attestation result, where available.</summary>
    public string? AttestationResult { get; set; }
}
