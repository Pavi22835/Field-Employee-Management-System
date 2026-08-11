namespace FEMS.Application.Employees;

public record CreateEmployeeRequest(
    string Username,
    string Email,
    string TemporaryPassword,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? Designation,
    string? Department,
    Guid? SupervisorId,
    DateOnly DateOfJoining,
    string Role); // must be "Employee" — this endpoint never provisions Admin/Supervisor/SuperAdmin accounts

public record UpdateEmployeeRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? Designation,
    string? Department,
    Guid? SupervisorId,
    bool IsActive);

public record EmployeeResponse(
    Guid Id,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string Email,
    string Username,
    string? PhoneNumber,
    string? Designation,
    string? Department,
    Guid? SupervisorId,
    bool IsActive,
    DateOnly DateOfJoining,
    IReadOnlyList<string> Roles,
    bool HasActiveDevice);
