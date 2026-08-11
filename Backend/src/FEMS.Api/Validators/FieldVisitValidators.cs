using FEMS.Application.FieldVisits;
using FluentValidation;

namespace FEMS.Api.Validators;

public class CheckInRequestValidator : AbstractValidator<CheckInRequest>
{
    public CheckInRequestValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
    }
}

public class RecordLocationRequestValidator : AbstractValidator<RecordLocationRequest>
{
    public RecordLocationRequestValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}

public class ReviewSubmissionRequestValidator : AbstractValidator<ReviewSubmissionRequest>
{
    public ReviewSubmissionRequestValidator()
    {
        RuleFor(x => x.ReviewStatus).Must(s => s is "Approved" or "Rejected")
            .WithMessage("ReviewStatus must be 'Approved' or 'Rejected'.");
        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}
