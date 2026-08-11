namespace FEMS.Application.Common.Interfaces;

/// <summary>Resolves the authenticated caller for audit stamping and authorization checks.</summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Username { get; }
    Guid? EmployeeId { get; }
    Guid? DeviceId { get; }
    IReadOnlyCollection<string> Roles { get; }
    string? IpAddress { get; }
    bool IsInRole(string role);
}
