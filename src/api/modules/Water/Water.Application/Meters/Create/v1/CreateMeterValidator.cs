using FluentValidation;

namespace FSH.Starter.WebApi.Water.Application.Meters.Create.v1;

public sealed class CreateMeterValidator : AbstractValidator<CreateMeterCommand>
{
    public CreateMeterValidator()
    {
        RuleFor(x => x.MeterNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Model).MaximumLength(100);
        RuleFor(x => x.InstallationDate).NotEmpty();
    }
}
