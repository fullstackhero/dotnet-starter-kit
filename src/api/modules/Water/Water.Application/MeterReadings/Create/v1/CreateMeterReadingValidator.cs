using FluentValidation;

namespace FSH.Starter.WebApi.Water.Application.MeterReadings.Create.v1;

public sealed class CreateMeterReadingValidator : AbstractValidator<CreateMeterReadingCommand>
{
    public CreateMeterReadingValidator()
    {
        RuleFor(x => x.ReadingValue).GreaterThan(0);
        RuleFor(x => x.Source).IsInEnum().NotEmpty();
    }
}
