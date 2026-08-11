using FEMS.Application.Common.Models;

namespace FEMS.Application.Admin;

public interface ISecurityAlertService
{
    Task<PagedResult<SecurityAlertResponse>> GetListAsync(int pageNumber, int pageSize, bool? unacknowledgedOnly, CancellationToken ct = default);
    Task AcknowledgeAsync(Guid alertId, Guid acknowledgedByUserId, CancellationToken ct = default);
}
