using FEMS.Domain.Common;

namespace FEMS.Domain.Entities;

/// <summary>Minimum supported app version enforcement (section 17 &amp; 21).</summary>
public class AppVersion : AuditableEntity
{
    public string Platform { get; set; } = "Android";
    public string MinimumSupportedVersion { get; set; } = default!;
    public string LatestVersion { get; set; } = default!;
    public bool ForceUpdate { get; set; }
    public string? ReleaseNotes { get; set; }
}
