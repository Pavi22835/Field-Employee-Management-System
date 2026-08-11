using FEMS.Application.Common.Interfaces;
using FEMS.Application.Common.Models;
using FEMS.Application.DynamicForms;
using FEMS.Domain.Entities;
using FEMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FEMS.Infrastructure.Services;

/// <summary>Section 10: configurable form templates for field data collection.</summary>
public class DynamicFormService : IDynamicFormService
{
    private static readonly string[] ValidFieldTypes = Enum.GetNames(typeof(FormFieldType));

    private readonly IApplicationDbContext _db;
    public DynamicFormService(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<DynamicFormResponse>> GetListAsync(int pageNumber, int pageSize, bool? activeOnly, CancellationToken ct = default)
    {
        var query = _db.DynamicForms.Include(f => f.Fields).Where(f => !f.IsDeleted).AsQueryable();
        if (activeOnly == true) query = query.Where(f => f.IsActive);

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(f => f.Name)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<DynamicFormResponse>
        {
            Items = items.Select(ToResponse).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<DynamicFormResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var form = await _db.DynamicForms.Include(f => f.Fields)
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(DynamicForm), id);
        return ToResponse(form);
    }

    public async Task<DynamicFormResponse> CreateAsync(CreateDynamicFormRequest request, CancellationToken ct = default)
    {
        ValidateFields(request.Fields);

        var form = new DynamicForm { Name = request.Name, Description = request.Description, IsActive = true, Version = 1 };
        foreach (var f in request.Fields.OrderBy(f => f.DisplayOrder))
        {
            form.Fields.Add(new FormField
            {
                Label = f.Label,
                FieldType = Enum.Parse<FormFieldType>(f.FieldType, true),
                IsRequired = f.IsRequired,
                DisplayOrder = f.DisplayOrder,
                OptionsJson = f.OptionsJson,
                ValidationRulesJson = f.ValidationRulesJson
            });
        }

        _db.DynamicForms.Add(form);
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(form.Id, ct);
    }

    public async Task<DynamicFormResponse> UpdateAsync(Guid id, UpdateDynamicFormRequest request, CancellationToken ct = default)
    {
        ValidateFields(request.Fields);

        var form = await _db.DynamicForms.Include(f => f.Fields)
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(DynamicForm), id);

        form.Name = request.Name;
        form.Description = request.Description;
        form.IsActive = request.IsActive;
        form.Version += 1;

        // Replace field set wholesale on update; existing FormSubmissions keep their
        // historical field references via FormSubmissionFile.FormFieldId (nullable), so
        // no data loss on resubmission of the form definition.
        foreach (var existing in form.Fields.ToList())
            _db.FormFields.Remove(existing);

        foreach (var f in request.Fields.OrderBy(f => f.DisplayOrder))
        {
            form.Fields.Add(new FormField
            {
                Label = f.Label,
                FieldType = Enum.Parse<FormFieldType>(f.FieldType, true),
                IsRequired = f.IsRequired,
                DisplayOrder = f.DisplayOrder,
                OptionsJson = f.OptionsJson,
                ValidationRulesJson = f.ValidationRulesJson
            });
        }

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    private static void ValidateFields(IReadOnlyList<FormFieldDto> fields)
    {
        if (fields.Count == 0) throw new AppException("A form must have at least one field.");
        foreach (var f in fields)
        {
            if (!ValidFieldTypes.Contains(f.FieldType, StringComparer.OrdinalIgnoreCase))
                throw new AppException($"Unsupported field type '{f.FieldType}'. Valid types: {string.Join(", ", ValidFieldTypes)}.");
        }
    }

    private static DynamicFormResponse ToResponse(DynamicForm f) => new(
        f.Id, f.Name, f.Description, f.IsActive, f.Version,
        f.Fields.OrderBy(x => x.DisplayOrder)
            .Select(x => new FormFieldDto(x.Id, x.Label, x.FieldType.ToString(), x.IsRequired, x.DisplayOrder, x.OptionsJson, x.ValidationRulesJson))
            .ToList());
}
