using FEMS.Domain.Common;

namespace FEMS.Domain.Entities;

/// <summary>Employee profile linked to a User account (section 17).</summary>
public class Employee : AuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    public string EmployeeCode { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? PhoneNumber { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }

    public Guid? SupervisorId { get; set; }
    public Employee? Supervisor { get; set; }

    public bool IsActive { get; set; } = true;
    public DateOnly DateOfJoining { get; set; }

    public ICollection<Device> Devices { get; set; } = new List<Device>();
    public ICollection<FieldAssignment> FieldAssignments { get; set; } = new List<FieldAssignment>();
    public ICollection<Attendance> AttendanceRecords { get; set; } = new List<Attendance>();
    public ICollection<Employee> DirectReports { get; set; } = new List<Employee>();
}
