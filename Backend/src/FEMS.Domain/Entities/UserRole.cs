using FEMS.Domain.Common;

namespace FEMS.Domain.Entities;

public class UserRole : AuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    public Guid RoleId { get; set; }
    public Role Role { get; set; } = default!;
}
