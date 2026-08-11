using FEMS.Domain.Common;

namespace FEMS.Domain.Entities;

/// <summary>Configurable form templates for field data collection (section 10).</summary>
public class DynamicForm : AuditableEntity
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;

    public ICollection<FormField> Fields { get; set; } = new List<FormField>();
    public ICollection<FieldAssignment> FieldAssignments { get; set; } = new List<FieldAssignment>();
}
