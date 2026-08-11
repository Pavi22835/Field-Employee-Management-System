using FEMS.Api.Common;
using FEMS.Application.Common.Interfaces;
using FEMS.Application.Common.Models;
using FEMS.Application.FieldAssignments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FEMS.Api.Controllers;

[ApiController]
[Route("api/field-assignments")]
[Authorize]
public class FieldAssignmentsController : ControllerBase
{
    private readonly IFieldAssignmentService _assignmentService;
    private readonly ICurrentUserService _currentUser;

    public FieldAssignmentsController(IFieldAssignmentService assignmentService, ICurrentUserService currentUser)
    {
        _assignmentService = assignmentService;
        _currentUser = currentUser;
    }

    /// <summary>GET /api/field-assignments — list, filterable by employee/date. Management roles only.</summary>
    [HttpGet]
    [Authorize(Policy = PolicyNames.ManagementOnly)]
    public async Task<ActionResult<ApiResponse<PagedResult<FieldAssignmentResponse>>>> GetList(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? employeeId = null, [FromQuery] DateOnly? date = null, CancellationToken ct = default)
    {
        var result = await _assignmentService.GetListAsync(pageNumber, pageSize, employeeId, date, ct);
        return Ok(ApiResponse<PagedResult<FieldAssignmentResponse>>.Ok(result));
    }

    /// <summary>GET /api/field-visits/my-visits — employee's assigned/today's visits (section 18).</summary>
    [HttpGet("/api/field-visits/my-visits")]
    [Authorize(Policy = PolicyNames.EmployeeOnly)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FieldAssignmentResponse>>>> MyVisits([FromQuery] DateOnly? date = null, CancellationToken ct = default)
    {
        var employeeId = _currentUser.EmployeeId ?? throw new Application.Common.Models.ForbiddenAppException("No employee profile linked to this account.");
        var result = await _assignmentService.GetMyVisitsAsync(employeeId, date ?? DateOnly.FromDateTime(DateTime.UtcNow), ct);
        return Ok(ApiResponse<IReadOnlyList<FieldAssignmentResponse>>.Ok(result));
    }

    /// <summary>POST /api/field-assignments — create a visit assignment (section 8 &amp; 18). Management roles only.</summary>
    [HttpPost]
    [Authorize(Policy = PolicyNames.ManagementOnly)]
    public async Task<ActionResult<ApiResponse<FieldAssignmentResponse>>> Create([FromBody] CreateFieldAssignmentRequest request, CancellationToken ct)
    {
        var result = await _assignmentService.CreateAsync(request, ct);
        return Ok(ApiResponse<FieldAssignmentResponse>.Ok(result, "Field assignment created."));
    }

    /// <summary>PUT /api/field-assignments/{id}/status — advance the visit status workflow (section 8.1).</summary>
    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<FieldAssignmentResponse>>> UpdateStatus(Guid id, [FromBody] UpdateFieldAssignmentStatusRequest request, CancellationToken ct)
    {
        var result = await _assignmentService.UpdateStatusAsync(id, request, ct);
        return Ok(ApiResponse<FieldAssignmentResponse>.Ok(result, "Status updated."));
    }
}
