using FEMS.Api.Common;
using FEMS.Application.Common.Interfaces;
using FEMS.Application.Common.Models;
using FEMS.Application.Devices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FEMS.Api.Controllers;

[ApiController]
[Route("api/devices")]
[Authorize]
public class DevicesController : ControllerBase
{
    private readonly IDeviceService _deviceService;
    private readonly ICurrentUserService _currentUser;

    public DevicesController(IDeviceService deviceService, ICurrentUserService currentUser)
    {
        _deviceService = deviceService;
        _currentUser = currentUser;
    }

    /// <summary>POST /api/devices/enroll — register a device to the calling employee (section 6 &amp; 18).</summary>
    [HttpPost("enroll")]
    [Authorize(Roles = RoleNames.Employee)]
    public async Task<ActionResult<ApiResponse<DeviceStatusResponse>>> Enroll([FromBody] EnrollDeviceRequest request, CancellationToken ct)
    {
        var employeeId = _currentUser.EmployeeId ?? throw new Application.Common.Models.ForbiddenAppException("No employee profile linked to this account.");
        var result = await _deviceService.EnrollAsync(employeeId, request, _currentUser.IpAddress, ct);
        return Ok(ApiResponse<DeviceStatusResponse>.Ok(result, "Device enrollment recorded. Pending admin approval."));
    }

    /// <summary>GET /api/devices/me — get current device registration status (section 18).</summary>
    [HttpGet("me")]
    [Authorize(Roles = RoleNames.Employee)]
    public async Task<ActionResult<ApiResponse<DeviceStatusResponse>>> Me(CancellationToken ct)
    {
        var employeeId = _currentUser.EmployeeId ?? throw new Application.Common.Models.ForbiddenAppException("No employee profile linked to this account.");
        var result = await _deviceService.GetMyDeviceAsync(employeeId, ct);
        return Ok(ApiResponse<DeviceStatusResponse>.Ok(result));
    }

    /// <summary>POST /api/devices/events — report device events: login, flight mode, etc. (section 18).</summary>
    [HttpPost("events")]
    public async Task<ActionResult<ApiResponse<object>>> ReportEvent([FromBody] ReportDeviceEventRequest request, CancellationToken ct)
    {
        var deviceId = _currentUser.DeviceId ?? throw new Application.Common.Models.ForbiddenAppException("No device bound to this session.");
        await _deviceService.ReportEventAsync(deviceId, _currentUser.EmployeeId, request, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Event recorded."));
    }

    /// <summary>POST /api/devices/heartbeat — reserved for later phase (heartbeat infrastructure, section 3.2).</summary>
    [HttpPost("heartbeat")]
    public IActionResult Heartbeat()
    {
        return StatusCode(StatusCodes.Status501NotImplemented,
            ApiResponse<object>.Fail("Heartbeat is reserved for a future phase (see roadmap Phase 6)."));
    }
}
