using FEMS.Domain.Common;

namespace FEMS.Domain.Entities;

/// <summary>
/// Attached files/photos for a submission (section 10 &amp; 11). Photos captured via the
/// in-app camera store timestamp, employee, device, visit, GPS and a file hash for integrity.
/// </summary>
public class FormSubmissionFile : AuditableEntity
{
    public Guid FormSubmissionId { get; set; }
    public FormSubmission FormSubmission { get; set; } = default!;

    public Guid? FormFieldId { get; set; }
    public FormField? FormField { get; set; }

    public string FileName { get; set; } = default!;
    public string StoragePath { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long FileSizeBytes { get; set; }

    /// <summary>SHA-256 hash of the file content, for integrity verification.</summary>
    public string FileHash { get; set; } = default!;

    public decimal? CapturedLatitude { get; set; }
    public decimal? CapturedLongitude { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
    public DateTimeOffset UploadedAt { get; set; }

    public Guid? DeviceId { get; set; }
    public Device? Device { get; set; }
}
