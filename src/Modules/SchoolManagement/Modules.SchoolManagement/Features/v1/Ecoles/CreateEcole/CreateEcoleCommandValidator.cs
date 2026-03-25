using FluentValidation;
using FSH.Modules.SchoolManagement.Contracts.v1.Ecoles.CreateEcole;

namespace FSH.Modules.SchoolManagement.Features.v1.Ecoles.CreateEcole;

public sealed class CreateEcoleCommandValidator : AbstractValidator<CreateEcoleCommand>
{
    public CreateEcoleCommandValidator()
    {
        RuleFor(x => x.Nom).NotEmpty().MaximumLength(256);
        RuleFor(x => x.CodeEcole).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Type).NotEmpty().Must(t => Enum.TryParse<Domain.TypeEcole>(t, ignoreCase: true, out _))
            .WithMessage("Type must be 'Public' or 'Prive'.");
        RuleFor(x => x.Telephone).MaximumLength(20).When(x => x.Telephone is not null);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => x.Email is not null);
        RuleFor(x => x.Adresse).MaximumLength(500).When(x => x.Adresse is not null);
        RuleFor(x => x.Region).MaximumLength(100).When(x => x.Region is not null);
        RuleFor(x => x.Ville).MaximumLength(100).When(x => x.Ville is not null);
    }
}
