using FEMS.Application.Common.Models;

namespace FEMS.Application.Employees;

public interface IEmployeeService
{
    Task<PagedResult<EmployeeResponse>> GetListAsync(int pageNumber, int pageSize, string? search, CancellationToken ct = default);
    Task<EmployeeResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<EmployeeResponse> CreateAsync(CreateEmployeeRequest request, CancellationToken ct = default);
    Task<EmployeeResponse> UpdateAsync(Guid id, UpdateEmployeeRequest request, CancellationToken ct = default);

    /// <summary>Soft delete/deactivate: blocks login (User.IsActive=false) and removes the
    /// employee from active lists (IsDeleted=true) without losing history.</summary>
    Task DeactivateAsync(Guid id, CancellationToken ct = default);
}
