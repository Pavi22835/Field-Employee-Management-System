using FEMS.Api.Common;
using FEMS.Application.Common.Models;
using FEMS.Application.FieldAreas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FEMS.Api.Controllers;

[ApiController]
[Route("api/field-areas")]
[Authorize]
public class FieldAreasController : ControllerBase
{
    private readonly IFieldAreaService _fieldAreaService;
    public FieldAreasController(IFieldAreaService fieldAreaService) => _fieldAreaService = fieldAreaService;

    /// <summary>GET /api/field-areas — list field areas (section 18).</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<FieldAreaResponse>>>> GetList(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, [FromQuery] bool? activeOnly = null, CancellationToken ct = default)
    {
        var result = await _fieldAreaService.GetListAsync(pageNumber, pageSize, activeOnly, ct);
        return Ok(ApiResponse<PagedResult<FieldAreaResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<FieldAreaResponse>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _fieldAreaService.GetByIdAsync(id, ct);
        return Ok(ApiResponse<FieldAreaResponse>.Ok(result));
    }

    /// <summary>POST /api/field-areas — create field area (section 7 &amp; 18). Admin/SuperAdmin only.</summary>
    [HttpPost]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    public async Task<ActionResult<ApiResponse<FieldAreaResponse>>> Create([FromBody] CreateFieldAreaRequest request, CancellationToken ct)
    {
        var result = await _fieldAreaService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<FieldAreaResponse>.Ok(result, "Field area created."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    public async Task<ActionResult<ApiResponse<FieldAreaResponse>>> Update(Guid id, [FromBody] UpdateFieldAreaRequest request, CancellationToken ct)
    {
        var result = await _fieldAreaService.UpdateAsync(id, request, ct);
        return Ok(ApiResponse<FieldAreaResponse>.Ok(result, "Field area updated."));
    }
}
