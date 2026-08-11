using FEMS.Domain.Common;
using FEMS.Domain.Enums;

namespace FEMS.Domain.Entities;

/// <summary>Daily attendance / presence records (section 17).</summary>
public class Attendance : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;

    public DateOnly AttendanceDate { get; set; }
    public DateTimeOffset? LoginTime { get; set; }
    public DateTimeOffset? LogoutTime { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Absent;
    public string? Remarks { get; set; }
}
