using FluentValidation;

namespace FSH.Starter.WebApi.Catalog.Application.Agencies.Update.v1;
public class UpdateAgencyCommandValidator : AbstractValidator<UpdateAgencyCommand>
{
    public UpdateAgencyCommandValidator()
    {
        RuleFor(a => a.Name).NotEmpty().MinimumLength(2).MaximumLength(100);
        RuleFor(a => a.Description).MaximumLength(1000);
    }
}