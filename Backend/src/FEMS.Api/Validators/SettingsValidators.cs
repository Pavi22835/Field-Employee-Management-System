using FEMS.Application.Settings;
using FluentValidation;

namespace FEMS.Api.Validators;

public class UpdateSystemSettingsRequestValidator : AbstractValidator<UpdateSystemSettingsRequest>
{
    public UpdateSystemSettingsRequestValidator()
    {
        RuleFor(x => x.LocationTrackingMode).NotEmpty();
        RuleFor(x => x.PeriodicTrackingIntervalSeconds).GreaterThan(0);
        RuleFor(x => x.DefaultGeofenceRadiusMeters).GreaterThan(0);
        RuleFor(x => x.SessionTimeoutMinutes).GreaterThan(0);
        RuleFor(x => x.MaxFailedLoginAttempts).GreaterThan(0);
        RuleFor(x => x.LockoutMinutes).GreaterThan(0);
        RuleFor(x => x.MinimumSupportedAppVersion).NotEmpty();
    }
}
