namespace FEMS.Application.Auth;

public record LoginRequest(string Username, string Password, Guid? DeviceAppInstallationId);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    Guid UserId,
    string Username,
    IReadOnlyList<string> Roles,
    Guid? EmployeeId,
    bool DeviceVerified);

public record RefreshRequest(string RefreshToken, Guid? DeviceAppInstallationId = null);

public record LogoutRequest(string RefreshToken);
