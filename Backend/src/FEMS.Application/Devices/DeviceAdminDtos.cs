namespace FEMS.Application.Devices;

/// <summary>Section 16: admin device list row and available actions.</summary>
public record DeviceListItemResponse(
    Guid Id,
    Guid? EmployeeId,
    string? EmployeeName,
    string? Model,
    string? Manufacturer,
    string? OsVersion,
    string Status,
    bool IsCompliant,
    DateTimeOffset? LastHeartbeatAt,
    string? AppVersion);

public record AssignDeviceRequest(Guid EmployeeId);
public record SendDeviceNotificationRequest(string Title, string Body);
public record MarkDeviceLostRequest(string? Notes);
