using FEMS.Api.Common;
using FEMS.Application.Common.Models;
using FEMS.Application.DynamicForms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FEMS.Api.Controllers;

[ApiController]
[Route("api/dynamic-forms")]
[Authorize]
public class DynamicFormsController : ControllerBase
{
    private readonly IDynamicFormService _formService;
    public DynamicFormsController(IDynamicFormService formService) => _formService = formService;

    /// <summary>GET /api/dynamic-forms — list form templates (section 10 &amp; 18). Any authenticated role (mobile app needs this too).</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<DynamicFormResponse>>>> GetList(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, [FromQuery] bool? activeOnly = true, CancellationToken ct = default)
    {
        var result = await _formService.GetListAsync(pageNumber, pageSize, activeOnly, ct);
        return Ok(ApiResponse<PagedResult<DynamicFormResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DynamicFormResponse>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _formService.GetByIdAsync(id, ct);
        return Ok(ApiResponse<DynamicFormResponse>.Ok(result));
    }

    /// <summary>POST /api/dynamic-forms — create a form template (section 10). Admin/SuperAdmin only.</summary>
    [HttpPost]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    public async Task<ActionResult<ApiResponse<DynamicFormResponse>>> Create([FromBody] CreateDynamicFormRequest request, CancellationToken ct)
    {
        var result = await _formService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<DynamicFormResponse>.Ok(result, "Form created."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    public async Task<ActionResult<ApiResponse<DynamicFormResponse>>> Update(Guid id, [FromBody] UpdateDynamicFormRequest request, CancellationToken ct)
    {
        var result = await _formService.UpdateAsync(id, request, ct);
        return Ok(ApiResponse<DynamicFormResponse>.Ok(result, "Form updated."));
    }
}
