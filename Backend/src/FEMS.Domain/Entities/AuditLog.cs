using FEMS.Domain.Common;

namespace FEMS.Domain.Entities;

/// <summary>Change history for sensitive entities/actions (section 5.1, 17, 19).</summary>
public class AuditLog : AuditableEntity
{
    public Guid? ActorUserId { get; set; }
    public User? ActorUser { get; set; }

    public string EntityName { get; set; } = default!;
    public string EntityId { get; set; } = default!;
    public string Action { get; set; } = default!;

    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }

    public string? IpAddress { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
