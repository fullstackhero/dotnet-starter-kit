using FluentValidation;

namespace FSH.Starter.WebApi.Water.Application.Tariffs.Update.v1;

public class UpdateTariffCommandValidator : AbstractValidator<UpdateTariffCommand>
{
    public UpdateTariffCommandValidator()
    {
        RuleFor(t => t.Name).NotEmpty().MaximumLength(100);
        RuleFor(t => t.Description).MaximumLength(1000);
    }
}
