namespace FEMS.Application.Users;

public record CreateSystemUserRequest(
    string Username,
    string Email,
    string TemporaryPassword,
    string Role); // must be "Admin" or "SuperAdmin" — see UserManagementService.AssignableRoles

public record SystemUserResponse(
    Guid Id,
    string Username,
    string Email,
    bool IsActive,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<string> Roles);
