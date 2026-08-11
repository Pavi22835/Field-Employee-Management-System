using FEMS.Domain.Common;
using FEMS.Domain.Enums;

namespace FEMS.Domain.Entities;

/// <summary>Field definitions belonging to a DynamicForm (section 10).</summary>
public class FormField : AuditableEntity
{
    public Guid DynamicFormId { get; set; }
    public DynamicForm DynamicForm { get; set; } = default!;

    public string Label { get; set; } = default!;
    public FormFieldType FieldType { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }

    /// <summary>JSON-encoded options for Dropdown/Radio/Checkbox field types.</summary>
    public string? OptionsJson { get; set; }
    public string? ValidationRulesJson { get; set; }
}
