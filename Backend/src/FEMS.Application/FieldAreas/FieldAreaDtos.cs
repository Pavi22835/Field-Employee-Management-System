namespace FEMS.Application.FieldAreas;

public record CreateFieldAreaRequest(
    string Name,
    string? Description,
    string? Address,
    decimal Latitude,
    decimal Longitude,
    int RadiusMeters,
    string EnforcementMode); // Mandatory | WarningOnly | Disabled

public record UpdateFieldAreaRequest(
    string Name,
    string? Description,
    string? Address,
    decimal Latitude,
    decimal Longitude,
    int RadiusMeters,
    string EnforcementMode,
    bool IsActive);

public record FieldAreaResponse(
    Guid Id,
    string Name,
    string? Description,
    string? Address,
    decimal Latitude,
    decimal Longitude,
    int RadiusMeters,
    string EnforcementMode,
    bool IsActive,
    int AssignedEmployeeCount);
