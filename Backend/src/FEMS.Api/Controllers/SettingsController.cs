using FEMS.Api.Common;
using FEMS.Application.Common.Models;
using FEMS.Application.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FEMS.Api.Controllers;

/// <summary>Section 21: admin configuration screens.</summary>
[ApiController]
[Route("api/admin/settings")]
[Authorize(Policy = PolicyNames.AdminOnly)]
public class SettingsController : ControllerBase
{
    private readonly ISystemSettingsService _settingsService;
    public SettingsController(ISystemSettingsService settingsService) => _settingsService = settingsService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<SystemSettingsResponse>>> Get(CancellationToken ct)
    {
        var result = await _settingsService.GetAsync(ct);
        return Ok(ApiResponse<SystemSettingsResponse>.Ok(result));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<SystemSettingsResponse>>> Update([FromBody] UpdateSystemSettingsRequest request, CancellationToken ct)
    {
        var result = await _settingsService.UpdateAsync(request, ct);
        return Ok(ApiResponse<SystemSettingsResponse>.Ok(result, "Settings updated."));
    }
}
