namespace FEMS.Application.Users;

public interface IUserManagementService
{
    Task<IReadOnlyList<SystemUserResponse>> GetListAsync(CancellationToken ct = default);
    Task<SystemUserResponse> CreateAsync(CreateSystemUserRequest request, CancellationToken ct = default);
    Task DeactivateAsync(Guid userId, CancellationToken ct = default);
}
