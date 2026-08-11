using FEMS.Application.FieldAreas;
using FluentValidation;

namespace FEMS.Api.Validators;

public class CreateFieldAreaRequestValidator : AbstractValidator<CreateFieldAreaRequest>
{
    public CreateFieldAreaRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.RadiusMeters).GreaterThan(0);
        RuleFor(x => x.EnforcementMode).NotEmpty();
    }
}

public class UpdateFieldAreaRequestValidator : AbstractValidator<UpdateFieldAreaRequest>
{
    public UpdateFieldAreaRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.RadiusMeters).GreaterThan(0);
        RuleFor(x => x.EnforcementMode).NotEmpty();
    }
}
