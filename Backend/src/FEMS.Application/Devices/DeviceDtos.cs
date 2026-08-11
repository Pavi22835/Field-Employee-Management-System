namespace FEMS.Application.Devices;

public record EnrollDeviceRequest(
    Guid AppInstallationId,
    string? AndroidId,
    string? Manufacturer,
    string? Model,
    string? OsVersion,
    string? AppVersion,
    string? PushToken);

public record DeviceStatusResponse(
    Guid DeviceId,
    string Status,
    Guid? AssignedEmployeeId,
    DateTimeOffset RegisteredAt,
    bool IsCompliant);

public record ReportDeviceEventRequest(string EventType, DateTimeOffset OccurredAt, string? Metadata);
