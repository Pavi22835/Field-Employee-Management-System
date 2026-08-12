using FEMS.Application.Users;
using FluentValidation;

namespace FEMS.Api.Validators;

public class CreateSystemUserRequestValidator : AbstractValidator<CreateSystemUserRequest>
{
    public CreateSystemUserRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.TemporaryPassword).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Role).NotEmpty().Must(r => r is "Admin" or "SuperAdmin")
            .WithMessage("System users can only be Admin or SuperAdmin.");
    }
}
