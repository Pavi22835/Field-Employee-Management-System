using FEMS.Domain.Common;

namespace FEMS.Domain.Entities;

/// <summary>Section 4: Super Admin, Admin, Supervisor/Manager, Employee.</summary>
public class Role : AuditableEntity
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
