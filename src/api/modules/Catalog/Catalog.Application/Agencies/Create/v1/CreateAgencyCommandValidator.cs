using FluentValidation;

namespace FSH.Starter.WebApi.Catalog.Application.Agencies.Create.v1;

public class CreateAgencyCommandValidator : AbstractValidator<CreateAgencyCommand>
{
    public CreateAgencyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("A valid email is required.");
        RuleFor(x => x.Telephone).NotEmpty().WithMessage("Telephone is required.");
        RuleFor(x => x.Address).NotEmpty().WithMessage("Address is required.");
    }
}
