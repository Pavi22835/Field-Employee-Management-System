namespace FEMS.Application.DynamicForms;

// Section 10: supported field types are validated against this list at the API boundary.
public record FormFieldDto(
    Guid? Id,
    string Label,
    string FieldType, // Text|Number|Dropdown|Checkbox|RadioButton|Date|Photo|Video|DocumentAttachment|GpsCoordinates|Signature
    bool IsRequired,
    int DisplayOrder,
    string? OptionsJson,
    string? ValidationRulesJson);

public record CreateDynamicFormRequest(string Name, string? Description, IReadOnlyList<FormFieldDto> Fields);

public record UpdateDynamicFormRequest(string Name, string? Description, bool IsActive, IReadOnlyList<FormFieldDto> Fields);

public record DynamicFormResponse(Guid Id, string Name, string? Description, bool IsActive, int Version, IReadOnlyList<FormFieldDto> Fields);
