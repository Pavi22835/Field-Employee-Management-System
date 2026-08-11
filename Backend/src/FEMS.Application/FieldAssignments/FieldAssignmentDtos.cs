namespace FEMS.Application.FieldAssignments;

public record CreateFieldAssignmentRequest(
    Guid EmployeeId,
    Guid FieldAreaId,
    DateOnly VisitDate,
    TimeOnly StartTime,
    TimeOnly ExpectedEndTime,
    int Priority,
    string? Instructions,
    string? RequiredInformation,
    Guid? DynamicFormId);

public record UpdateFieldAssignmentStatusRequest(string Status); // Assigned|Accepted|Started|InProgress|Completed|Cancelled|Missed

public record FieldAssignmentResponse(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    Guid FieldAreaId,
    string FieldAreaName,
    decimal FieldAreaLatitude,
    decimal FieldAreaLongitude,
    int FieldAreaRadiusMeters,
    DateOnly VisitDate,
    TimeOnly StartTime,
    TimeOnly ExpectedEndTime,
    int Priority,
    string? Instructions,
    Guid? DynamicFormId,
    string Status);
