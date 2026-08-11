using FEMS.Domain.Common;
using FEMS.Domain.Enums;

namespace FEMS.Domain.Entities;

/// <summary>Login/logout, flight mode, power-off and other device events (section 6, 17, 18).</summary>
public class DeviceEvent : AuditableEntity
{
    public Guid DeviceId { get; set; }
    public Device Device { get; set; } = default!;

    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public DeviceEventType EventType { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? Metadata { get; set; }
}
