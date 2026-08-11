namespace FEMS.Application.Admin;

// Section 13.3, 16, 20.2: alert records. Full real-time push dashboard is reserved for
// Phase 6; this is the pull-based list + acknowledge workflow available now.
public record SecurityAlertResponse(
    Guid Id,
    string AlertType,
    string Severity,
    string Message,
    Guid? EmployeeId,
    string? EmployeeName,
    Guid? DeviceId,
    bool IsAcknowledged,
    DateTimeOffset? AcknowledgedAt,
    DateTimeOffset CreatedAt);
