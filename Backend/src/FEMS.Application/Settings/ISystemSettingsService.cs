namespace FEMS.Application.Settings;

public interface ISystemSettingsService
{
    Task<SystemSettingsResponse> GetAsync(CancellationToken ct = default);
    Task<SystemSettingsResponse> UpdateAsync(UpdateSystemSettingsRequest request, CancellationToken ct = default);
    Task<LocationTrackingPolicyResponse> GetTrackingPolicyAsync(CancellationToken ct = default);
}
