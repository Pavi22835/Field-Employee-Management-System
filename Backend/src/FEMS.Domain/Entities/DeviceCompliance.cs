using FEMS.Domain.Common;

namespace FEMS.Domain.Entities;

/// <summary>Compliance status snapshots per device (section 17).</summary>
public class DeviceCompliance : AuditableEntity
{
    public Guid DeviceId { get; set; }
    public Device Device { get; set; } = default!;

    public bool IsCompliant { get; set; }
    public string? MinAppVersionCheck { get; set; }
    public string? OsVersionCheck { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset EvaluatedAt { get; set; }
}
