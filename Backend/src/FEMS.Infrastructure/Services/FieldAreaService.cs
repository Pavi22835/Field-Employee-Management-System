using FEMS.Application.Common.Interfaces;
using FEMS.Application.Common.Models;
using FEMS.Application.FieldAreas;
using FEMS.Domain.Entities;
using FEMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FEMS.Infrastructure.Services;

/// <summary>Section 7: field area (geofence) management.</summary>
public class FieldAreaService : IFieldAreaService
{
    private readonly IApplicationDbContext _db;
    public FieldAreaService(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<FieldAreaResponse>> GetListAsync(int pageNumber, int pageSize, bool? activeOnly, CancellationToken ct = default)
    {
        var query = _db.FieldAreas.Include(a => a.Assignments).Where(a => !a.IsDeleted).AsQueryable();
        if (activeOnly == true) query = query.Where(a => a.IsActive);

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(a => a.Name)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(a => ToResponse(a)).ToListAsync(ct);

        return new PagedResult<FieldAreaResponse> { Items = items, PageNumber = pageNumber, PageSize = pageSize, TotalCount = total };
    }

    public async Task<FieldAreaResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var area = await _db.FieldAreas.Include(a => a.Assignments)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(FieldArea), id);
        return ToResponse(area);
    }

    public async Task<FieldAreaResponse> CreateAsync(CreateFieldAreaRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<GeofenceEnforcementMode>(request.EnforcementMode, true, out var mode))
            throw new AppException("EnforcementMode must be one of: Mandatory, WarningOnly, Disabled.");

        var area = new FieldArea
        {
            Name = request.Name,
            Description = request.Description,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            RadiusMeters = request.RadiusMeters,
            EnforcementMode = mode,
            IsActive = true
        };
        _db.FieldAreas.Add(area);
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(area.Id, ct);
    }

    public async Task<FieldAreaResponse> UpdateAsync(Guid id, UpdateFieldAreaRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<GeofenceEnforcementMode>(request.EnforcementMode, true, out var mode))
            throw new AppException("EnforcementMode must be one of: Mandatory, WarningOnly, Disabled.");

        var area = await _db.FieldAreas.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(FieldArea), id);

        area.Name = request.Name;
        area.Description = request.Description;
        area.Address = request.Address;
        area.Latitude = request.Latitude;
        area.Longitude = request.Longitude;
        area.RadiusMeters = request.RadiusMeters;
        area.EnforcementMode = mode;
        area.IsActive = request.IsActive;

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    private static FieldAreaResponse ToResponse(FieldArea a) => new(
        a.Id, a.Name, a.Description, a.Address, a.Latitude, a.Longitude, a.RadiusMeters,
        a.EnforcementMode.ToString(), a.IsActive, a.Assignments.Select(x => x.EmployeeId).Distinct().Count());
}
