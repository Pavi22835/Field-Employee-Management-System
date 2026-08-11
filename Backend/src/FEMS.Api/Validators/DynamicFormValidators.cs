using FEMS.Application.DynamicForms;
using FluentValidation;

namespace FEMS.Api.Validators;

public class FormFieldDtoValidator : AbstractValidator<FormFieldDto>
{
    public FormFieldDtoValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FieldType).NotEmpty();
    }
}

public class CreateDynamicFormRequestValidator : AbstractValidator<CreateDynamicFormRequest>
{
    public CreateDynamicFormRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleForEach(x => x.Fields).SetValidator(new FormFieldDtoValidator());
    }
}

public class UpdateDynamicFormRequestValidator : AbstractValidator<UpdateDynamicFormRequest>
{
    public UpdateDynamicFormRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleForEach(x => x.Fields).SetValidator(new FormFieldDtoValidator());
    }
}
