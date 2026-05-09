using FluentValidation;

namespace FSH.Starter.WebApi.Water.Application.Tariffs.Create.v1;

public sealed class CreateTariffCommandValidator : AbstractValidator<CreateTariffCommand>
{
    public CreateTariffCommandValidator()
    {
        RuleFor(t => t.Name).NotEmpty().MaximumLength(100);
        RuleFor(t => t.Description).MaximumLength(1000);
        RuleFor(t => t.RatePerUnit).GreaterThanOrEqualTo(0);
        RuleFor(t => t.FixedCharge).GreaterThanOrEqualTo(0);
    }
}
