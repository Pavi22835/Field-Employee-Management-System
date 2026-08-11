using FEMS.Application.FieldAssignments;
using FluentValidation;

namespace FEMS.Api.Validators;

public class CreateFieldAssignmentRequestValidator : AbstractValidator<CreateFieldAssignmentRequest>
{
    public CreateFieldAssignmentRequestValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.FieldAreaId).NotEmpty();
        RuleFor(x => x.Priority).InclusiveBetween(0, 10);
    }
}

public class UpdateFieldAssignmentStatusRequestValidator : AbstractValidator<UpdateFieldAssignmentStatusRequest>
{
    public UpdateFieldAssignmentStatusRequestValidator()
    {
        RuleFor(x => x.Status).NotEmpty();
    }
}
