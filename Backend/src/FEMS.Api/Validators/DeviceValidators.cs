using FEMS.Application.Devices;
using FluentValidation;

namespace FEMS.Api.Validators;

public class EnrollDeviceRequestValidator : AbstractValidator<EnrollDeviceRequest>
{
    public EnrollDeviceRequestValidator()
    {
        RuleFor(x => x.AppInstallationId).NotEmpty();
        RuleFor(x => x.Manufacturer).MaximumLength(100);
        RuleFor(x => x.Model).MaximumLength(100);
    }
}
