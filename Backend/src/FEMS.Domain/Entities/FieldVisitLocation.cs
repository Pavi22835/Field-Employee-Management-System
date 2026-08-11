using FEMS.Domain.Common;

namespace FEMS.Domain.Entities;

/// <summary>Location points captured during a visit, per the configured tracking mode (section 15).</summary>
public class FieldVisitLocation : AuditableEntity
{
    public Guid FieldVisitId { get; set; }
    public FieldVisit FieldVisit { get; set; } = default!;

    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public double? AccuracyMeters { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
}
