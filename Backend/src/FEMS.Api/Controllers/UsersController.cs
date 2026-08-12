using FEMS.Api.Common;
using FEMS.Application.Common.Models;
using FEMS.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FEMS.Api.Controllers;

/// <summary>
/// Section 4: management of system (Admin/SuperAdmin) accounts — distinct from
/// EmployeesController, which provisions Employee/Supervisor accounts. SuperAdmin-only
/// end to end, so an Admin can never create or promote another Admin/SuperAdmin.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = PolicyNames.SuperAdminOnly)]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;
    public UsersController(IUserManagementService userManagementService) => _userManagementService = userManagementService;

    /// <summary>GET /api/admin/users — list Admin/SuperAdmin system accounts.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SystemUserResponse>>>> GetList(CancellationToken ct)
    {
        var result = await _userManagementService.GetListAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<SystemUserResponse>>.Ok(result));
    }

    /// <summary>POST /api/admin/users — create an Admin or SuperAdmin account.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SystemUserResponse>>> Create([FromBody] CreateSystemUserRequest request, CancellationToken ct)
    {
        var result = await _userManagementService.CreateAsync(request, ct);
        return Ok(ApiResponse<SystemUserResponse>.Ok(result, "System user created."));
    }

    /// <summary>DELETE /api/admin/users/{id} — deactivate a system account (blocks login).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Deactivate(Guid id, CancellationToken ct)
    {
        await _userManagementService.DeactivateAsync(id, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "System user deactivated."));
    }
}
