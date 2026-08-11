using System.Text.Json;
using FEMS.Api.Common;
using FEMS.Application.Common.Interfaces;
using FEMS.Application.Common.Models;
using FEMS.Application.FieldVisits;
using FEMS.Application.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FEMS.Api.Controllers;

/// <summary>
/// Sections 9-12 &amp; 18: check-in, dynamic form/photo submission, and visit completion
/// (Employee role, ownership-scoped) plus management visibility/review of visits and
/// submissions (Admin/Supervisor, section 4). Authorization is per-action since the two
/// halves of this controller require disjoint roles.
/// </summary>
[ApiController]
[Route("api/field-visits")]
[Authorize]
public class FieldVisitsController : ControllerBase
{
    private const long MaxFileSizeBytes = 25 * 1024 * 1024; // matches Nginx/IIS 25M body-size guidance in the deployment notes

    private readonly IFieldVisitService _fieldVisitService;
    private readonly ICurrentUserService _currentUser;
    private readonly ISystemSettingsService _settingsService;

    public FieldVisitsController(IFieldVisitService fieldVisitService, ICurrentUserService currentUser, ISystemSettingsService settingsService)
    {
        _fieldVisitService = fieldVisitService;
        _currentUser = currentUser;
        _settingsService = settingsService;
    }

    /// <summary>GET /api/field-visits/tracking-policy — the org's location tracking mode (section 15), for the mobile app.</summary>
    [HttpGet("tracking-policy")]
    public async Task<ActionResult<ApiResponse<LocationTrackingPolicyResponse>>> TrackingPolicy(CancellationToken ct)
    {
        var result = await _settingsService.GetTrackingPolicyAsync(ct);
        return Ok(ApiResponse<LocationTrackingPolicyResponse>.Ok(result));
    }

    /// <summary>POST /api/field-visits/{assignmentId}/check-in — "Start Visit" (section 9 &amp; 18).</summary>
    [HttpPost("{assignmentId:guid}/check-in")]
    [Authorize(Policy = PolicyNames.EmployeeOnly)]
    public async Task<ActionResult<ApiResponse<FieldVisitResponse>>> CheckIn(Guid assignmentId, [FromBody] CheckInRequest request, CancellationToken ct)
    {
        var employeeId = RequireEmployeeId();
        var result = await _fieldVisitService.CheckInAsync(assignmentId, employeeId, _currentUser.DeviceId, request, ct);
        return Ok(ApiResponse<FieldVisitResponse>.Ok(result, result.GeofenceMessage));
    }

    /// <summary>
    /// POST /api/field-visits/{visitId}/submit — submit dynamic form data + photos (section 10, 11 &amp; 18).
    /// multipart/form-data: dynamicFormId (guid), valuesJson (JSON array of {formFieldId, value}),
    /// files (0..n), fileMetaJson (JSON array of {formFieldId?, latitude?, longitude?} aligned by file order).
    /// </summary>
    [HttpPost("{visitId:guid}/submit")]
    [RequestSizeLimit(MaxFileSizeBytes * 10)]
    [Authorize(Policy = PolicyNames.EmployeeOnly)]
    public async Task<ActionResult<ApiResponse<FormSubmissionResponse>>> Submit(
        Guid visitId,
        [FromForm] Guid dynamicFormId,
        [FromForm] string? valuesJson,
        [FromForm] string? fileMetaJson,
        [FromForm] List<IFormFile>? files,
        CancellationToken ct)
    {
        var employeeId = RequireEmployeeId();

        var values = string.IsNullOrWhiteSpace(valuesJson)
            ? new List<SubmitVisitFormValueDto>()
            : JsonSerializer.Deserialize<List<SubmitVisitFormValueDto>>(valuesJson, JsonOpts) ?? new();

        var fileMeta = string.IsNullOrWhiteSpace(fileMetaJson)
            ? new List<FileMetaDto>()
            : JsonSerializer.Deserialize<List<FileMetaDto>>(fileMetaJson, JsonOpts) ?? new();

        var fileTuples = new List<(Guid? formFieldId, string fileName, string contentType, Stream content, decimal? lat, decimal? lng)>();
        if (files is not null)
        {
            for (var i = 0; i < files.Count; i++)
            {
                if (files[i].Length > MaxFileSizeBytes)
                    throw new AppException($"File '{files[i].FileName}' exceeds the {MaxFileSizeBytes / (1024 * 1024)}MB limit.");

                var meta = i < fileMeta.Count ? fileMeta[i] : null;
                fileTuples.Add((meta?.FormFieldId, files[i].FileName, files[i].ContentType, files[i].OpenReadStream(), meta?.Latitude, meta?.Longitude));
            }
        }

        var result = await _fieldVisitService.SubmitAsync(visitId, employeeId, dynamicFormId, values, fileTuples, _currentUser.DeviceId, ct);
        return Ok(ApiResponse<FormSubmissionResponse>.Ok(result, "Submission recorded."));
    }

    /// <summary>POST /api/field-visits/{visitId}/complete — "Complete Visit" (section 12 &amp; 18).</summary>
    [HttpPost("{visitId:guid}/complete")]
    [Authorize(Policy = PolicyNames.EmployeeOnly)]
    public async Task<ActionResult<ApiResponse<FieldVisitResponse>>> Complete(Guid visitId, [FromBody] CompleteVisitRequest request, CancellationToken ct)
    {
        var employeeId = RequireEmployeeId();
        var result = await _fieldVisitService.CompleteAsync(visitId, employeeId, request, ct);
        return Ok(ApiResponse<FieldVisitResponse>.Ok(result, "Visit completed."));
    }

    /// <summary>POST /api/field-visits/{visitId}/locations — record a location point during an active visit (section 15).</summary>
    [HttpPost("{visitId:guid}/locations")]
    [Authorize(Policy = PolicyNames.EmployeeOnly)]
    public async Task<ActionResult<ApiResponse<RecordLocationResponse>>> RecordLocation(Guid visitId, [FromBody] RecordLocationRequest request, CancellationToken ct)
    {
        var employeeId = RequireEmployeeId();
        var result = await _fieldVisitService.RecordLocationAsync(visitId, employeeId, request, ct);
        return Ok(ApiResponse<RecordLocationResponse>.Ok(result, result.Message));
    }

    /// <summary>GET /api/field-visits — management (Admin/Supervisor) list of visits (section 4 &amp; 13).</summary>
    [HttpGet]
    [Authorize(Policy = PolicyNames.ManagementOnly)]
    public async Task<ActionResult<ApiResponse<PagedResult<FieldVisitSummaryResponse>>>> GetList(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? employeeId = null, [FromQuery] DateOnly? date = null, CancellationToken ct = default)
    {
        var result = await _fieldVisitService.GetVisitListAsync(pageNumber, pageSize, employeeId, date, ct);
        return Ok(ApiResponse<PagedResult<FieldVisitSummaryResponse>>.Ok(result));
    }

    /// <summary>GET /api/field-visits/{id} — management detail view: check-in/out, submissions, files (section 4 &amp; 12).</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = PolicyNames.ManagementOnly)]
    public async Task<ActionResult<ApiResponse<FieldVisitDetailResponse>>> GetDetail(Guid id, CancellationToken ct)
    {
        var result = await _fieldVisitService.GetVisitDetailAsync(id, ct);
        return Ok(ApiResponse<FieldVisitDetailResponse>.Ok(result));
    }

    /// <summary>POST /api/field-visits/submissions/{submissionId}/review — approve/reject a submission (section 4).</summary>
    [HttpPost("submissions/{submissionId:guid}/review")]
    [Authorize(Policy = PolicyNames.ManagementOnly)]
    public async Task<ActionResult<ApiResponse<SubmissionDetail>>> ReviewSubmission(Guid submissionId, [FromBody] ReviewSubmissionRequest request, CancellationToken ct)
    {
        var reviewerUserId = _currentUser.UserId ?? throw new UnauthorizedAppException();
        var result = await _fieldVisitService.ReviewSubmissionAsync(submissionId, reviewerUserId, request, ct);
        return Ok(ApiResponse<SubmissionDetail>.Ok(result, $"Submission {result.ReviewStatus.ToLowerInvariant()}."));
    }

    /// <summary>GET /api/field-visits/files/{fileId} — stream a submitted photo/document for management review.</summary>
    [HttpGet("files/{fileId:guid}")]
    [Authorize(Policy = PolicyNames.ManagementOnly)]
    public async Task<IActionResult> GetFile(Guid fileId, CancellationToken ct)
    {
        var (content, contentType, fileName) = await _fieldVisitService.OpenSubmissionFileAsync(fileId, ct);
        return File(content, contentType, fileName);
    }

    private Guid RequireEmployeeId() =>
        _currentUser.EmployeeId ?? throw new ForbiddenAppException("No employee profile linked to this account.");

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private record FileMetaDto(Guid? FormFieldId, decimal? Latitude, decimal? Longitude);
}
