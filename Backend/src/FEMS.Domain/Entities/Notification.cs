using FEMS.Domain.Common;
using FEMS.Domain.Enums;

namespace FEMS.Domain.Entities;

/// <summary>Push/in-app notification log (section 20).</summary>
public class Notification : AuditableEntity
{
    public Guid? RecipientUserId { get; set; }
    public User? RecipientUser { get; set; }

    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
    public NotificationChannel Channel { get; set; } = NotificationChannel.Push;

    public bool IsRead { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public string? DataJson { get; set; }
}
