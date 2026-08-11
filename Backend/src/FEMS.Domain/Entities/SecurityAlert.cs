using FEMS.Domain.Common;
using FEMS.Domain.Enums;

namespace FEMS.Domain.Entities;

/// <summary>
/// Alert records — reserved for the future real-time Security Alert Dashboard (section 3.2, 13.3, 20.2).
/// Populated now for unregistered-device attempts and compliance changes; full dashboard is Phase 6.
/// </summary>
public class SecurityAlert : AuditableEntity
{
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public Guid? DeviceId { get; set; }
    public Device? Device { get; set; }

    public string AlertType { get; set; } = default!;
    public SecurityAlertSeverity Severity { get; set; } = SecurityAlertSeverity.Info;
    public string Message { get; set; } = default!;

    public bool IsAcknowledged { get; set; }
    public DateTimeOffset? AcknowledgedAt { get; set; }
    public Guid? AcknowledgedBy { get; set; }
}
