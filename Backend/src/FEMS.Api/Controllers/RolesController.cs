using FEMS.Api.Common;
using FEMS.Application.Common.Models;
using FEMS.Application.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FEMS.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Policy = PolicyNames.ManagementOnly)]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;
    public RolesController(IRoleService roleService) => _roleService = roleService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleResponse>>>> GetAll(CancellationToken ct)
    {
        var result = await _roleService.GetAllAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<RoleResponse>>.Ok(result));
    }
}
