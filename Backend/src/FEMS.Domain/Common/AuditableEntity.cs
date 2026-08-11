namespace FEMS.Domain.Common;

/// <summary>
/// Base class for all entities. Provides the audit trail and soft-delete
/// columns mandated by section 17 of the requirements document
/// (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted) for every table.
/// </summary>
public abstract class AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}
