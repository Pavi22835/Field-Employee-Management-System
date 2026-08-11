using FEMS.Domain.Common;
using FEMS.Domain.Enums;

namespace FEMS.Domain.Entities;

/// <summary>Actual visit execution records: check-in/out, status (section 9 &amp; 12).</summary>
public class FieldVisit : AuditableEntity
{
    public Guid FieldAssignmentId { get; set; }
    public FieldAssignment FieldAssignment { get; set; } = default!;

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;

    public Guid? DeviceId { get; set; }
    public Device? Device { get; set; }

    public VisitStatus Status { get; set; } = VisitStatus.Assigned;

    public DateTimeOffset? CheckInAt { get; set; }
    public decimal? CheckInLatitude { get; set; }
    public decimal? CheckInLongitude { get; set; }
    public double? CheckInDistanceMeters { get; set; }

    public DateTimeOffset? CheckOutAt { get; set; }
    public decimal? CheckOutLatitude { get; set; }
    public decimal? CheckOutLongitude { get; set; }

    public string? Remarks { get; set; }

    public ICollection<FieldVisitLocation> LocationPoints { get; set; } = new List<FieldVisitLocation>();
    public ICollection<FormSubmission> FormSubmissions { get; set; } = new List<FormSubmission>();
}
