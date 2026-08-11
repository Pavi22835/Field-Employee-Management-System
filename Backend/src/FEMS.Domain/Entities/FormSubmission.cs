using FEMS.Domain.Common;
using FEMS.Domain.Enums;

namespace FEMS.Domain.Entities;

/// <summary>Submitted data for a field visit (section 10 &amp; 12).</summary>
public class FormSubmission : AuditableEntity
{
    public Guid FieldVisitId { get; set; }
    public FieldVisit FieldVisit { get; set; } = default!;

    public Guid DynamicFormId { get; set; }
    public DynamicForm DynamicForm { get; set; } = default!;

    /// <summary>JSON payload of fieldId -&gt; submitted value, for non-file field types.</summary>
    public string DataJson { get; set; } = "{}";

    public DateTimeOffset SubmittedAt { get; set; }

    /// <summary>Section 4: Supervisor/Manager review workflow for submitted field data.</summary>
    public SubmissionReviewStatus ReviewStatus { get; set; } = SubmissionReviewStatus.Pending;
    public Guid? ReviewedByUserId { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewComment { get; set; }

    public ICollection<FormSubmissionFile> Files { get; set; } = new List<FormSubmissionFile>();
}
