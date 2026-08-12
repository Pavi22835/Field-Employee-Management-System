using System.Text.Json;
using FEMS.Application.Common.Interfaces;
using FEMS.Application.Common.Models;
using FEMS.Application.FieldVisits;
using FEMS.Domain.Common;
using FEMS.Domain.Entities;
using FEMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FEMS.Infrastructure.Services;

/// <summary>Sections 9-12 &amp; 15: field check-in, dynamic form/photo submission, visit completion, and location tracking.</summary>
public class FieldVisitService : IFieldVisitService
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _dateTime;
    private readonly IFileStorageService _fileStorage;
    private readonly ICurrentUserService _currentUser;

    public FieldVisitService(IApplicationDbContext db, IDateTimeProvider dateTime, IFileStorageService fileStorage, ICurrentUserService currentUser)
    {
        _db = db;
        _dateTime = dateTime;
        _fileStorage = fileStorage;
        _currentUser = currentUser;
    }

    public async Task<FieldVisitResponse> CheckInAsync(Guid fieldAssignmentId, Guid employeeId, Guid? deviceId, CheckInRequest request, CancellationToken ct = default)
    {
        var assignment = await _db.FieldAssignments.Include(a => a.FieldArea)
            .FirstOrDefaultAsync(a => a.Id == fieldAssignmentId && !a.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(FieldAssignment), fieldAssignmentId);

        if (assignment.EmployeeId != employeeId)
            throw new ForbiddenAppException("This assignment does not belong to you.");

        if (assignment.Status is VisitStatus.Completed or VisitStatus.Cancelled or VisitStatus.Missed)
            throw new AppException($"Cannot check in: assignment is already {assignment.Status}.", 409);

        // A FieldVisit row already exists once check-in starts (Started/Accepted/InProgress),
        // enforced by the IX_FieldVisits_FieldAssignmentId unique constraint — check explicitly
        // so a retry/double-tap gets a clean 409 instead of an unhandled unique-violation 500.
        var alreadyCheckedIn = await _db.FieldVisits.AnyAsync(v => v.FieldAssignmentId == assignment.Id && !v.IsDeleted, ct);
        if (alreadyCheckedIn)
            throw new AppException("A visit has already been started for this assignment.", 409);

        double? distance = null;
        var geofenceSatisfied = true;
        var geofenceMessage = "Location not supplied; geofence not evaluated.";

        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            distance = GeofenceCalculator.DistanceMeters(
                (double)request.Latitude.Value, (double)request.Longitude.Value,
                (double)assignment.FieldArea.Latitude, (double)assignment.FieldArea.Longitude);

            geofenceSatisfied = distance <= assignment.FieldArea.RadiusMeters;
            geofenceMessage = geofenceSatisfied
                ? "CHECK-IN SUCCESSFUL."
                : "You are outside the assigned field area. Please move to the assigned location before starting the visit.";

            if (!geofenceSatisfied && assignment.FieldArea.EnforcementMode == GeofenceEnforcementMode.Mandatory)
                throw new AppException(geofenceMessage, 422);
            // WarningOnly: proceed but the response carries GeofenceSatisfied=false and the message.
            // Disabled: enforcement is skipped entirely regardless of distance (still reported for visibility).
            if (assignment.FieldArea.EnforcementMode == GeofenceEnforcementMode.Disabled)
                geofenceSatisfied = true;
        }

        var visit = new FieldVisit
        {
            FieldAssignmentId = assignment.Id,
            EmployeeId = employeeId,
            DeviceId = deviceId,
            Status = VisitStatus.Started,
            CheckInAt = _dateTime.UtcNow,
            CheckInLatitude = request.Latitude,
            CheckInLongitude = request.Longitude,
            CheckInDistanceMeters = distance
        };
        _db.FieldVisits.Add(visit);

        assignment.Status = VisitStatus.Started;

        await _db.SaveChangesAsync(ct);

        return ToResponse(visit, geofenceSatisfied, geofenceMessage);
    }

    public async Task<FormSubmissionResponse> SubmitAsync(
        Guid fieldVisitId, Guid employeeId, Guid dynamicFormId, IReadOnlyList<SubmitVisitFormValueDto> values,
        IReadOnlyList<(Guid? formFieldId, string fileName, string contentType, Stream content, decimal? lat, decimal? lng)> files,
        Guid? deviceId, CancellationToken ct = default)
    {
        var visit = await _db.FieldVisits.FirstOrDefaultAsync(v => v.Id == fieldVisitId && !v.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(FieldVisit), fieldVisitId);

        if (visit.EmployeeId != employeeId)
            throw new ForbiddenAppException("This visit does not belong to you.");

        if (visit.Status is VisitStatus.Completed or VisitStatus.Cancelled)
            throw new AppException($"Cannot submit data: visit is already {visit.Status}.", 409);

        var form = await _db.DynamicForms.Include(f => f.Fields)
            .FirstOrDefaultAsync(f => f.Id == dynamicFormId && !f.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(DynamicForm), dynamicFormId);

        // Section 10 & 12: IsRequired is enforced here, not just client-side — a caller
        // going around the mobile app (or a client bug) must not be able to persist a
        // submission, and therefore complete the visit, without the form's required data.
        ValidateRequiredFields(form.Fields, values, files);

        var dataJson = JsonSerializer.Serialize(values.ToDictionary(v => v.FormFieldId.ToString(), v => v.Value));

        var submission = new FormSubmission
        {
            FieldVisitId = fieldVisitId,
            DynamicFormId = dynamicFormId,
            DataJson = dataJson,
            SubmittedAt = _dateTime.UtcNow
        };
        _db.FormSubmissions.Add(submission);
        await _db.SaveChangesAsync(ct);

        foreach (var file in files)
        {
            var stored = await _fileStorage.SaveAsync(file.content, file.fileName, $"visits/{fieldVisitId}", ct);

            _db.FormSubmissionFiles.Add(new FormSubmissionFile
            {
                FormSubmissionId = submission.Id,
                FormFieldId = file.formFieldId,
                FileName = file.fileName,
                StoragePath = stored.StoragePath,
                ContentType = file.contentType,
                FileSizeBytes = stored.SizeBytes,
                FileHash = stored.FileHash,
                CapturedLatitude = file.lat,
                CapturedLongitude = file.lng,
                CapturedAt = _dateTime.UtcNow,
                UploadedAt = _dateTime.UtcNow,
                DeviceId = deviceId
            });
        }

        if (visit.Status == VisitStatus.Started)
            visit.Status = VisitStatus.InProgress;

        await _db.SaveChangesAsync(ct);

        var fileCount = await _db.FormSubmissionFiles.CountAsync(f => f.FormSubmissionId == submission.Id, ct);
        return new FormSubmissionResponse(submission.Id, submission.FieldVisitId, submission.DynamicFormId, submission.SubmittedAt, fileCount);
    }

    // Photo/Video/DocumentAttachment answer with an uploaded file, not a `values` entry;
    // every other field type (Text, Number, Dropdown, Checkbox, RadioButton, Date,
    // GpsCoordinates, Signature) answers with a plain string value.
    private static readonly HashSet<FormFieldType> FileBackedFieldTypes = new()
    {
        FormFieldType.Photo, FormFieldType.Video, FormFieldType.DocumentAttachment
    };

    private static void ValidateRequiredFields(
        IEnumerable<FormField> fields,
        IReadOnlyList<SubmitVisitFormValueDto> values,
        IReadOnlyList<(Guid? formFieldId, string fileName, string contentType, Stream content, decimal? lat, decimal? lng)> files)
    {
        var missingLabels = fields
            .Where(f => f.IsRequired)
            .Where(f => FileBackedFieldTypes.Contains(f.FieldType)
                ? !files.Any(file => file.formFieldId == f.Id)
                : string.IsNullOrWhiteSpace(values.FirstOrDefault(v => v.FormFieldId == f.Id)?.Value))
            .Select(f => f.Label)
            .ToList();

        if (missingLabels.Count > 0)
            throw new AppException($"Please complete all required fields: {string.Join(", ", missingLabels)}.", 422);
    }

    public async Task<FieldVisitResponse> CompleteAsync(Guid fieldVisitId, Guid employeeId, CompleteVisitRequest request, CancellationToken ct = default)
    {
        var visit = await _db.FieldVisits.Include(v => v.FieldAssignment)
            .FirstOrDefaultAsync(v => v.Id == fieldVisitId && !v.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(FieldVisit), fieldVisitId);

        if (visit.EmployeeId != employeeId)
            throw new ForbiddenAppException("This visit does not belong to you.");

        if (visit.Status is VisitStatus.Completed or VisitStatus.Cancelled)
            throw new AppException($"Visit is already {visit.Status}.", 409);

        // Section 12: at least one form submission is required before a visit can be completed
        // if the assignment specifies a required dynamic form.
        if (visit.FieldAssignment.DynamicFormId.HasValue)
        {
            var hasSubmission = await _db.FormSubmissions.AnyAsync(s => s.FieldVisitId == visit.Id, ct);
            if (!hasSubmission)
                throw new AppException("Required field information has not been submitted for this visit.", 422);
        }

        visit.Status = VisitStatus.Completed;
        visit.CheckOutAt = _dateTime.UtcNow;
        visit.CheckOutLatitude = request.Latitude;
        visit.CheckOutLongitude = request.Longitude;
        visit.Remarks = request.Remarks;

        visit.FieldAssignment.Status = VisitStatus.Completed;

        await _db.SaveChangesAsync(ct);

        return ToResponse(visit, true, "Visit completed.");
    }

    private static FieldVisitResponse ToResponse(FieldVisit v, bool geofenceSatisfied, string geofenceMessage) => new(
        v.Id, v.FieldAssignmentId, v.EmployeeId, v.Status.ToString(), v.CheckInAt, v.CheckInDistanceMeters,
        geofenceSatisfied, geofenceMessage, v.CheckOutAt, v.Remarks);

    public async Task<RecordLocationResponse> RecordLocationAsync(Guid fieldVisitId, Guid employeeId, RecordLocationRequest request, CancellationToken ct = default)
    {
        var visit = await _db.FieldVisits.FirstOrDefaultAsync(v => v.Id == fieldVisitId && !v.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(FieldVisit), fieldVisitId);

        if (visit.EmployeeId != employeeId)
            throw new ForbiddenAppException("This visit does not belong to you.");

        if (visit.Status is VisitStatus.Completed or VisitStatus.Cancelled)
            return new RecordLocationResponse(false, $"Visit is already {visit.Status}; location not recorded.");

        var settings = await _db.SystemSettings.FirstOrDefaultAsync(ct);
        var mode = settings?.LocationTrackingMode ?? LocationTrackingMode.VisitBased;

        // Section 15: "Visit Based" only captures check-in/check-out points (already stored on
        // FieldVisit itself), so intermediate pings are intentionally not persisted in that mode.
        if (mode == LocationTrackingMode.VisitBased)
            return new RecordLocationResponse(false, "Location tracking mode is Visit Based; intermediate points are not recorded.");

        _db.FieldVisitLocations.Add(new FieldVisitLocation
        {
            FieldVisitId = fieldVisitId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            AccuracyMeters = request.AccuracyMeters,
            CapturedAt = request.CapturedAt
        });

        await _db.SaveChangesAsync(ct);
        return new RecordLocationResponse(true, "Location recorded.");
    }

    private bool IsUnrestrictedManagement => _currentUser.IsInRole("SuperAdmin") || _currentUser.IsInRole("Admin");

    public async Task<PagedResult<FieldVisitSummaryResponse>> GetVisitListAsync(int pageNumber, int pageSize, Guid? employeeId, DateOnly? date, CancellationToken ct = default)
    {
        // IgnoreQueryFilters() + an explicit !v.IsDeleted below: EF Core's global soft-delete
        // filter on Employee/FieldArea would otherwise cascade through Include() and silently
        // drop a visit from this list the moment its Employee is deactivated — defeating the
        // whole point of soft-delete, which is to keep historical/audit records visible.
        var query = _db.FieldVisits
            .IgnoreQueryFilters()
            .Include(v => v.Employee)
            .Include(v => v.FieldAssignment).ThenInclude(a => a.FieldArea)
            .Include(v => v.FormSubmissions)
            .Where(v => !v.IsDeleted)
            .AsQueryable();

        // Section 4: Supervisor/Manager is scoped to "assigned employees" only.
        if (!IsUnrestrictedManagement && _currentUser.IsInRole("Supervisor"))
        {
            var supervisorId = _currentUser.EmployeeId;
            query = query.Where(v => v.Employee.SupervisorId == supervisorId);
        }

        if (employeeId.HasValue) query = query.Where(v => v.EmployeeId == employeeId.Value);
        if (date.HasValue)
        {
            var start = date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var end = start.AddDays(1);
            query = query.Where(v => v.CreatedAt >= start && v.CreatedAt < end);
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(v => v.CreatedAt)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(v => new FieldVisitSummaryResponse(
                v.Id, v.FieldAssignmentId, v.EmployeeId, v.Employee.FirstName + " " + v.Employee.LastName,
                v.FieldAssignment.FieldAreaId, v.FieldAssignment.FieldArea.Name, v.Status.ToString(),
                v.CheckInAt, v.CheckOutAt, v.FormSubmissions.Count,
                v.FormSubmissions.Any(s => s.ReviewStatus == SubmissionReviewStatus.Rejected) ? "Rejected"
                    : v.FormSubmissions.Any() && v.FormSubmissions.All(s => s.ReviewStatus == SubmissionReviewStatus.Approved) ? "Approved"
                    : "Pending"))
            .ToListAsync(ct);

        return new PagedResult<FieldVisitSummaryResponse> { Items = items, PageNumber = pageNumber, PageSize = pageSize, TotalCount = total };
    }

    public async Task<FieldVisitDetailResponse> GetVisitDetailAsync(Guid fieldVisitId, CancellationToken ct = default)
    {
        var visit = await _db.FieldVisits
            .IgnoreQueryFilters() // see comment in GetVisitListAsync
            .Include(v => v.Employee)
            .Include(v => v.FieldAssignment).ThenInclude(a => a.FieldArea)
            .Include(v => v.FormSubmissions).ThenInclude(s => s.DynamicForm).ThenInclude(f => f.Fields)
            .Include(v => v.FormSubmissions).ThenInclude(s => s.Files)
            .FirstOrDefaultAsync(v => v.Id == fieldVisitId && !v.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(FieldVisit), fieldVisitId);

        if (!IsUnrestrictedManagement && _currentUser.IsInRole("Supervisor") && visit.Employee.SupervisorId != _currentUser.EmployeeId)
            throw new ForbiddenAppException("You can only review visits for your own team.");

        var reviewerIds = visit.FormSubmissions.Where(s => s.ReviewedByUserId.HasValue).Select(s => s.ReviewedByUserId!.Value).Distinct().ToList();
        var reviewerNames = reviewerIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Users.Where(u => reviewerIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Username, ct);

        var submissions = visit.FormSubmissions.Select(s =>
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(s.DataJson) ?? new();
            var fieldsById = s.DynamicForm.Fields.ToDictionary(f => f.Id);
            var valueSummaries = values.Select(kv =>
            {
                var fieldId = Guid.Parse(kv.Key);
                var field = fieldsById.GetValueOrDefault(fieldId);
                return new FormFieldValueSummary(fieldId, field?.Label ?? "(deleted field)", field?.FieldType.ToString() ?? "Text", kv.Value);
            }).ToList();

            var files = s.Files.Select(f => new SubmissionFileSummary(
                f.Id, f.FormFieldId, f.FileName, f.ContentType, f.FileSizeBytes,
                f.CapturedLatitude, f.CapturedLongitude, f.CapturedAt)).ToList();

            return new SubmissionDetail(
                s.Id, s.DynamicFormId, s.DynamicForm.Name, s.SubmittedAt, valueSummaries, files,
                s.ReviewStatus.ToString(),
                s.ReviewedByUserId.HasValue ? reviewerNames.GetValueOrDefault(s.ReviewedByUserId.Value) : null,
                s.ReviewedAt, s.ReviewComment);
        }).ToList();

        return new FieldVisitDetailResponse(
            visit.Id, visit.FieldAssignmentId, visit.EmployeeId, $"{visit.Employee.FirstName} {visit.Employee.LastName}",
            visit.FieldAssignment.FieldAreaId, visit.FieldAssignment.FieldArea.Name, visit.Status.ToString(),
            visit.CheckInAt, visit.CheckInLatitude, visit.CheckInLongitude, visit.CheckInDistanceMeters,
            visit.CheckOutAt, visit.Remarks, submissions);
    }

    public async Task<SubmissionDetail> ReviewSubmissionAsync(Guid submissionId, Guid reviewerUserId, ReviewSubmissionRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<SubmissionReviewStatus>(request.ReviewStatus, true, out var status) || status == SubmissionReviewStatus.Pending)
            throw new AppException("ReviewStatus must be 'Approved' or 'Rejected'.");

        var submission = await _db.FormSubmissions
            .IgnoreQueryFilters() // see comment in GetVisitListAsync — must survive Employee deactivation
            .Include(s => s.DynamicForm).ThenInclude(f => f.Fields)
            .Include(s => s.Files)
            .Include(s => s.FieldVisit).ThenInclude(v => v.Employee)
            .FirstOrDefaultAsync(s => s.Id == submissionId && !s.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(FormSubmission), submissionId);

        if (!IsUnrestrictedManagement && _currentUser.IsInRole("Supervisor") && submission.FieldVisit.Employee.SupervisorId != _currentUser.EmployeeId)
            throw new ForbiddenAppException("You can only review submissions for your own team.");

        submission.ReviewStatus = status;
        submission.ReviewedByUserId = reviewerUserId;
        submission.ReviewedAt = _dateTime.UtcNow;
        submission.ReviewComment = request.Comment;
        await _db.SaveChangesAsync(ct);

        var reviewer = await _db.Users.FirstOrDefaultAsync(u => u.Id == reviewerUserId, ct);
        var values = JsonSerializer.Deserialize<Dictionary<string, string>>(submission.DataJson) ?? new();
        var fieldsById = submission.DynamicForm.Fields.ToDictionary(f => f.Id);
        var valueSummaries = values.Select(kv =>
        {
            var fieldId = Guid.Parse(kv.Key);
            var field = fieldsById.GetValueOrDefault(fieldId);
            return new FormFieldValueSummary(fieldId, field?.Label ?? "(deleted field)", field?.FieldType.ToString() ?? "Text", kv.Value);
        }).ToList();
        var files = submission.Files.Select(f => new SubmissionFileSummary(
            f.Id, f.FormFieldId, f.FileName, f.ContentType, f.FileSizeBytes,
            f.CapturedLatitude, f.CapturedLongitude, f.CapturedAt)).ToList();

        return new SubmissionDetail(
            submission.Id, submission.DynamicFormId, submission.DynamicForm.Name, submission.SubmittedAt,
            valueSummaries, files, submission.ReviewStatus.ToString(), reviewer?.Username, submission.ReviewedAt, submission.ReviewComment);
    }

    public async Task<(Stream Content, string ContentType, string FileName)> OpenSubmissionFileAsync(Guid fileId, CancellationToken ct = default)
    {
        var file = await _db.FormSubmissionFiles
            .IgnoreQueryFilters() // see comment in GetVisitListAsync — must survive Employee deactivation
            .Include(f => f.FormSubmission).ThenInclude(s => s.FieldVisit).ThenInclude(v => v.Employee)
            .FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(FormSubmissionFile), fileId);

        if (!IsUnrestrictedManagement && _currentUser.IsInRole("Supervisor") && file.FormSubmission.FieldVisit.Employee.SupervisorId != _currentUser.EmployeeId)
            throw new ForbiddenAppException("You can only view files for your own team's visits.");

        return (_fileStorage.OpenRead(file.StoragePath), file.ContentType, file.FileName);
    }
}
