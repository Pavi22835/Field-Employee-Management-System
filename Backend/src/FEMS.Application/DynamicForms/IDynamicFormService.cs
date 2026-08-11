using FEMS.Application.Common.Models;

namespace FEMS.Application.DynamicForms;

public interface IDynamicFormService
{
    Task<PagedResult<DynamicFormResponse>> GetListAsync(int pageNumber, int pageSize, bool? activeOnly, CancellationToken ct = default);
    Task<DynamicFormResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<DynamicFormResponse> CreateAsync(CreateDynamicFormRequest request, CancellationToken ct = default);
    Task<DynamicFormResponse> UpdateAsync(Guid id, UpdateDynamicFormRequest request, CancellationToken ct = default);
}
