using System.Security.Claims;
using FEMS.Application.Common.Interfaces;

namespace FEMS.Api.Common;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Username => Principal?.FindFirstValue(ClaimTypes.Name);

    public Guid? EmployeeId
    {
        get
        {
            var value = Principal?.FindFirstValue("employeeId");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Guid? DeviceId
    {
        get
        {
            var value = Principal?.FindFirstValue("deviceId");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? new List<string>();

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
}
