using FEMS.Api.Common;
using FEMS.Application.Admin;
using FEMS.Application.Common.Interfaces;
using FEMS.Application.Common.Models;
using FEMS.Application.Devices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FEMS.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = PolicyNames.AdminOnly)]
public class AdminController : ControllerBase
{
    private readonly IAdminDashboardService _dashboardService;
    private readonly IDeviceAdminService _deviceAdminService;
    private readonly ISecurityAlertService _securityAlertService;
    private readonly ICurrentUserService _currentUser;

    public AdminController(
        IAdminDashboardService dashboardService, IDeviceAdminService deviceAdminService,
        ISecurityAlertService securityAlertService, ICurrentUserService currentUser)
    {
        _dashboardService = dashboardService;
        _deviceAdminService = deviceAdminService;
        _securityAlertService = securityAlertService;
        _currentUser = currentUser;
    }

    /// <summary>GET /api/admin/dashboard — aggregated dashboard statistics (section 13 &amp; 18).</summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<DashboardResponse>>> Dashboard(CancellationToken ct)
    {
        var result = await _dashboardService.GetDashboardAsync(ct);
        return Ok(ApiResponse<DashboardResponse>.Ok(result));
    }

    /// <summary>GET /api/admin/devices — device list for section 16.</summary>
    [HttpGet("devices")]
    public async Task<ActionResult<ApiResponse<PagedResult<DeviceListItemResponse>>>> Devices(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null, CancellationToken ct = default)
    {
        var result = await _deviceAdminService.GetListAsync(pageNumber, pageSize, status, ct);
        return Ok(ApiResponse<PagedResult<DeviceListItemResponse>>.Ok(result));
    }

    /// <summary>POST /api/admin/devices/{id}/approve — approve device/replacement (section 18).</summary>
    [HttpPost("devices/{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<object>>> ApproveDevice(Guid id, CancellationToken ct)
    {
        await _deviceAdminService.ApproveAsync(id, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Device approved."));
    }

    /// <summary>POST /api/admin/devices/{id}/revoke — revoke a device (section 18).</summary>
    [HttpPost("devices/{id:guid}/revoke")]
    public async Task<ActionResult<ApiResponse<object>>> RevokeDevice(Guid id, [FromQuery] string? reason, CancellationToken ct)
    {
        await _deviceAdminService.RevokeAsync(id, reason, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Device revoked."));
    }

    /// <summary>POST /api/admin/devices/{id}/assign — assign device to an employee (section 16).</summary>
    [HttpPost("devices/{id:guid}/assign")]
    public async Task<ActionResult<ApiResponse<object>>> AssignDevice(Guid id, [FromBody] AssignDeviceRequest request, CancellationToken ct)
    {
        await _deviceAdminService.AssignAsync(id, request, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Device assigned."));
    }

    /// <summary>POST /api/admin/devices/{id}/unassign — unassign device (section 16).</summary>
    [HttpPost("devices/{id:guid}/unassign")]
    public async Task<ActionResult<ApiResponse<object>>> UnassignDevice(Guid id, CancellationToken ct)
    {
        await _deviceAdminService.UnassignAsync(id, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Device unassigned."));
    }

    /// <summary>POST /api/admin/devices/{id}/mark-lost — mark device lost (section 16).</summary>
    [HttpPost("devices/{id:guid}/mark-lost")]
    public async Task<ActionResult<ApiResponse<object>>> MarkLost(Guid id, [FromBody] MarkDeviceLostRequest request, CancellationToken ct)
    {
        await _deviceAdminService.MarkLostAsync(id, request, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Device marked lost."));
    }

    /// <summary>POST /api/admin/devices/{id}/notify — send notification (section 16).</summary>
    [HttpPost("devices/{id:guid}/notify")]
    public async Task<ActionResult<ApiResponse<object>>> Notify(Guid id, [FromBody] SendDeviceNotificationRequest request, CancellationToken ct)
    {
        await _deviceAdminService.SendNotificationAsync(id, request, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Notification queued."));
    }

    /// <summary>POST /api/admin/employees/{employeeId}/lock — lock employee account (section 16).</summary>
    [HttpPost("employees/{employeeId:guid}/lock")]
    public async Task<ActionResult<ApiResponse<object>>> LockEmployee(Guid employeeId, CancellationToken ct)
    {
        await _deviceAdminService.LockEmployeeAccountAsync(employeeId, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Employee account locked."));
    }

    /// <summary>POST /api/admin/employees/{employeeId}/force-logout — force logout (section 16).</summary>
    [HttpPost("employees/{employeeId:guid}/force-logout")]
    public async Task<ActionResult<ApiResponse<object>>> ForceLogout(Guid employeeId, CancellationToken ct)
    {
        await _deviceAdminService.ForceLogoutAsync(employeeId, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Employee sessions terminated."));
    }

    /// <summary>
    /// GET /api/admin/alerts — list device/security alerts (section 13.3, 16, 18, 20.2).
    /// Pull-based list + acknowledge for now; a real-time push dashboard is reserved for Phase 6.
    /// </summary>
    [HttpGet("alerts")]
    public async Task<ActionResult<ApiResponse<PagedResult<SecurityAlertResponse>>>> Alerts(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] bool? unacknowledgedOnly = null, CancellationToken ct = default)
    {
        var result = await _securityAlertService.GetListAsync(pageNumber, pageSize, unacknowledgedOnly, ct);
        return Ok(ApiResponse<PagedResult<SecurityAlertResponse>>.Ok(result));
    }

    /// <summary>POST /api/admin/alerts/{id}/acknowledge — acknowledge a security alert.</summary>
    [HttpPost("alerts/{id:guid}/acknowledge")]
    public async Task<ActionResult<ApiResponse<object>>> AcknowledgeAlert(Guid id, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new Application.Common.Models.UnauthorizedAppException();
        await _securityAlertService.AcknowledgeAsync(id, userId, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Alert acknowledged."));
    }
}
