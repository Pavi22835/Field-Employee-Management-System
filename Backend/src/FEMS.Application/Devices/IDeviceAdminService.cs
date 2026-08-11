using FEMS.Application.Common.Models;

namespace FEMS.Application.Devices;

/// <summary>Section 16: admin device management actions.</summary>
public interface IDeviceAdminService
{
    Task<PagedResult<DeviceListItemResponse>> GetListAsync(int pageNumber, int pageSize, string? statusFilter, CancellationToken ct = default);
    Task ApproveAsync(Guid deviceId, CancellationToken ct = default);
    Task RevokeAsync(Guid deviceId, string? reason, CancellationToken ct = default);
    Task AssignAsync(Guid deviceId, AssignDeviceRequest request, CancellationToken ct = default);
    Task UnassignAsync(Guid deviceId, CancellationToken ct = default);
    Task MarkLostAsync(Guid deviceId, MarkDeviceLostRequest request, CancellationToken ct = default);
    Task LockEmployeeAccountAsync(Guid employeeId, CancellationToken ct = default);
    Task ForceLogoutAsync(Guid employeeId, CancellationToken ct = default);
    Task SendNotificationAsync(Guid deviceId, SendDeviceNotificationRequest request, CancellationToken ct = default);
}
