namespace FEMS.Application.Devices;

public interface IDeviceService
{
    Task<DeviceStatusResponse> EnrollAsync(Guid employeeId, EnrollDeviceRequest request, string? ipAddress, CancellationToken ct = default);
    Task<DeviceStatusResponse> GetMyDeviceAsync(Guid employeeId, CancellationToken ct = default);
    Task ReportEventAsync(Guid deviceId, Guid? employeeId, ReportDeviceEventRequest request, CancellationToken ct = default);
}
