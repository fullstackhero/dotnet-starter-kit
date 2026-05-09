using FluentValidation;

namespace FSH.Starter.WebApi.Water.Application.Meters.Update.v1;

public class UpdateMeterCommandValidator : AbstractValidator<UpdateMeterCommand>
{
    public UpdateMeterCommandValidator()
    {
        RuleFor(x => x.Model).MaximumLength(100);
    }
}
