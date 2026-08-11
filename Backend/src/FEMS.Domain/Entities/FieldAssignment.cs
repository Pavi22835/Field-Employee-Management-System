using FEMS.Domain.Common;
using FEMS.Domain.Enums;

namespace FEMS.Domain.Entities;

/// <summary>Planned visits assigned to employees (section 8).</summary>
public class FieldAssignment : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;

    public Guid FieldAreaId { get; set; }
    public FieldArea FieldArea { get; set; } = default!;

    public DateOnly VisitDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly ExpectedEndTime { get; set; }

    public int Priority { get; set; }
    public string? Instructions { get; set; }
    public string? RequiredInformation { get; set; }

    public VisitStatus Status { get; set; } = VisitStatus.Assigned;

    public Guid? DynamicFormId { get; set; }
    public DynamicForm? DynamicForm { get; set; }

    public FieldVisit? FieldVisit { get; set; }
}
