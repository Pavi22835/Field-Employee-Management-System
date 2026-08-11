using FEMS.Application.Common.Models;

namespace FEMS.Application.FieldAssignments;

public interface IFieldAssignmentService
{
    Task<PagedResult<FieldAssignmentResponse>> GetListAsync(int pageNumber, int pageSize, Guid? employeeId, DateOnly? date, CancellationToken ct = default);
    Task<IReadOnlyList<FieldAssignmentResponse>> GetMyVisitsAsync(Guid employeeId, DateOnly date, CancellationToken ct = default);
    Task<FieldAssignmentResponse> CreateAsync(CreateFieldAssignmentRequest request, CancellationToken ct = default);
    Task<FieldAssignmentResponse> UpdateStatusAsync(Guid id, UpdateFieldAssignmentStatusRequest request, CancellationToken ct = default);
}
