namespace FEMS.Application.Roles;

public interface IRoleService
{
    Task<IReadOnlyList<RoleResponse>> GetAllAsync(CancellationToken ct = default);
}
