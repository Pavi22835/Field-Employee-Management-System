using FEMS.Application.Common.Models;

namespace FEMS.Application.FieldAreas;

public interface IFieldAreaService
{
    Task<PagedResult<FieldAreaResponse>> GetListAsync(int pageNumber, int pageSize, bool? activeOnly, CancellationToken ct = default);
    Task<FieldAreaResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<FieldAreaResponse> CreateAsync(CreateFieldAreaRequest request, CancellationToken ct = default);
    Task<FieldAreaResponse> UpdateAsync(Guid id, UpdateFieldAreaRequest request, CancellationToken ct = default);
}
