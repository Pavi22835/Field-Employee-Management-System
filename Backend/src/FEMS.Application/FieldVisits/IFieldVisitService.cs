using FEMS.Application.Common.Models;

namespace FEMS.Application.FieldVisits;

public interface IFieldVisitService
{
    /// <summary>Section 9: "Start Visit" — validates employee/device/assignment and (if location supplied) the geofence.</summary>
    Task<FieldVisitResponse> CheckInAsync(Guid fieldAssignmentId, Guid employeeId, Guid? deviceId, CheckInRequest request, CancellationToken ct = default);

    /// <summary>Section 10 &amp; 11: submit dynamic form data plus any captured photos/files for a visit.</summary>
    Task<FormSubmissionResponse> SubmitAsync(Guid fieldVisitId, Guid employeeId, Guid dynamicFormId, IReadOnlyList<SubmitVisitFormValueDto> values,
        IReadOnlyList<(Guid? formFieldId, string fileName, string contentType, Stream content, decimal? lat, decimal? lng)> files,
        Guid? deviceId, CancellationToken ct = default);

    /// <summary>Section 12: "Complete Visit" — validates required fields were submitted and closes out the visit.</summary>
    Task<FieldVisitResponse> CompleteAsync(Guid fieldVisitId, Guid employeeId, CompleteVisitRequest request, CancellationToken ct = default);

    /// <summary>Section 15: records a location point during an active visit, respecting the org's configured tracking mode.</summary>
    Task<RecordLocationResponse> RecordLocationAsync(Guid fieldVisitId, Guid employeeId, RecordLocationRequest request, CancellationToken ct = default);

    /// <summary>Section 4/13: management (Admin/Supervisor) list of visits, scoped to the caller's team for Supervisor.</summary>
    Task<PagedResult<FieldVisitSummaryResponse>> GetVisitListAsync(int pageNumber, int pageSize, Guid? employeeId, DateOnly? date, CancellationToken ct = default);

    /// <summary>Section 12: management detail view of a visit — check-in/out, submitted data, and files, for review.</summary>
    Task<FieldVisitDetailResponse> GetVisitDetailAsync(Guid fieldVisitId, CancellationToken ct = default);

    /// <summary>Section 4: Supervisor/Manager approves or rejects a submitted form.</summary>
    Task<SubmissionDetail> ReviewSubmissionAsync(Guid submissionId, Guid reviewerUserId, ReviewSubmissionRequest request, CancellationToken ct = default);

    /// <summary>Streams a previously-uploaded submission file (photo/document) for management review.</summary>
    Task<(Stream Content, string ContentType, string FileName)> OpenSubmissionFileAsync(Guid fileId, CancellationToken ct = default);
}
