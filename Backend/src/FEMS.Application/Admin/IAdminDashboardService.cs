namespace FEMS.Application.Admin;

public interface IAdminDashboardService
{
    Task<DashboardResponse> GetDashboardAsync(CancellationToken ct = default);
}
