namespace FEMS.Application.FieldVisits;

public record CheckInRequest(decimal? Latitude, decimal? Longitude);

public record SubmitVisitFormValueDto(Guid FormFieldId, string Value);

public record CompleteVisitRequest(decimal? Latitude, decimal? Longitude, string? Remarks);

public record FieldVisitResponse(
    Guid Id,
    Guid FieldAssignmentId,
    Guid EmployeeId,
    string Status,
    DateTimeOffset? CheckInAt,
    double? CheckInDistanceMeters,
    bool GeofenceSatisfied,
    string GeofenceMessage,
    DateTimeOffset? CheckOutAt,
    string? Remarks);

public record FormSubmissionResponse(Guid Id, Guid FieldVisitId, Guid DynamicFormId, DateTimeOffset SubmittedAt, int FileCount);

// Section 15: location tracking policy.
public record RecordLocationRequest(decimal Latitude, decimal Longitude, double? AccuracyMeters, DateTimeOffset CapturedAt);

public record RecordLocationResponse(bool Recorded, string Message);

// Section 4 & 12: management (Admin/Supervisor) visibility into visit execution and
// submission review — "Admin/Supervisor can see and review the updated visit."
public record FieldVisitSummaryResponse(
    Guid Id,
    Guid FieldAssignmentId,
    Guid EmployeeId,
    string EmployeeName,
    Guid FieldAreaId,
    string FieldAreaName,
    string Status,
    DateTimeOffset? CheckInAt,
    DateTimeOffset? CheckOutAt,
    int SubmissionCount,
    string OverallReviewStatus); // Pending if any submission is Pending/none exist, else Approved/Rejected

public record SubmissionFileSummary(
    Guid Id, Guid? FormFieldId, string FileName, string ContentType, long FileSizeBytes,
    decimal? CapturedLatitude, decimal? CapturedLongitude, DateTimeOffset CapturedAt);

public record FormFieldValueSummary(Guid FormFieldId, string Label, string FieldType, string? Value);

public record SubmissionDetail(
    Guid Id, Guid DynamicFormId, string DynamicFormName, DateTimeOffset SubmittedAt,
    IReadOnlyList<FormFieldValueSummary> Values, IReadOnlyList<SubmissionFileSummary> Files,
    string ReviewStatus, string? ReviewedByUsername, DateTimeOffset? ReviewedAt, string? ReviewComment);

public record FieldVisitDetailResponse(
    Guid Id,
    Guid FieldAssignmentId,
    Guid EmployeeId,
    string EmployeeName,
    Guid FieldAreaId,
    string FieldAreaName,
    string Status,
    DateTimeOffset? CheckInAt,
    decimal? CheckInLatitude,
    decimal? CheckInLongitude,
    double? CheckInDistanceMeters,
    DateTimeOffset? CheckOutAt,
    string? Remarks,
    IReadOnlyList<SubmissionDetail> Submissions);

public record ReviewSubmissionRequest(string ReviewStatus, string? Comment); // ReviewStatus: Approved|Rejected
