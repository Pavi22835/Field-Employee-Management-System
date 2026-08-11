using FEMS.Api.Common;
using FEMS.Application.Common.Models;
using FEMS.Application.Employees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FEMS.Api.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize(Policy = PolicyNames.ManagementOnly)]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    public EmployeesController(IEmployeeService employeeService) => _employeeService = employeeService;

    /// <summary>GET /api/employees — list employees (section 18).</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<EmployeeResponse>>>> GetList(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, CancellationToken ct = default)
    {
        var result = await _employeeService.GetListAsync(pageNumber, pageSize, search, ct);
        return Ok(ApiResponse<PagedResult<EmployeeResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<EmployeeResponse>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _employeeService.GetByIdAsync(id, ct);
        return Ok(ApiResponse<EmployeeResponse>.Ok(result));
    }

    /// <summary>POST /api/employees — create employee (section 18). Admin/SuperAdmin only.</summary>
    [HttpPost]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    public async Task<ActionResult<ApiResponse<EmployeeResponse>>> Create([FromBody] CreateEmployeeRequest request, CancellationToken ct)
    {
        var result = await _employeeService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<EmployeeResponse>.Ok(result, "Employee created."));
    }

    /// <summary>PUT /api/employees/{id} — update employee (section 18). Admin/SuperAdmin only.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    public async Task<ActionResult<ApiResponse<EmployeeResponse>>> Update(Guid id, [FromBody] UpdateEmployeeRequest request, CancellationToken ct)
    {
        var result = await _employeeService.UpdateAsync(id, request, ct);
        return Ok(ApiResponse<EmployeeResponse>.Ok(result, "Employee updated."));
    }

    /// <summary>DELETE /api/employees/{id} — soft delete/deactivate: blocks login and removes
    /// the employee from active lists without losing history. Admin/SuperAdmin only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    public async Task<ActionResult<ApiResponse<object>>> Deactivate(Guid id, CancellationToken ct)
    {
        await _employeeService.DeactivateAsync(id, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Employee deactivated."));
    }
}
