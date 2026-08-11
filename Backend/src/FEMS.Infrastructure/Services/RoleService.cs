using FEMS.Application.Common.Interfaces;
using FEMS.Application.Roles;
using Microsoft.EntityFrameworkCore;

namespace FEMS.Infrastructure.Services;

public class RoleService : IRoleService
{
    private readonly IApplicationDbContext _db;
    public RoleService(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<RoleResponse>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Roles.Where(r => !r.IsDeleted)
            .Select(r => new RoleResponse(r.Id, r.Name, r.Description))
            .ToListAsync(ct);
}
