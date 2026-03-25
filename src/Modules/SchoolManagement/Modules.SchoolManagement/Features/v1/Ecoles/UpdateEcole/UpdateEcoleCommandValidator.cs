using FluentValidation;
using FSH.Modules.SchoolManagement.Contracts.v1.Ecoles.UpdateEcole;

namespace FSH.Modules.SchoolManagement.Features.v1.Ecoles.UpdateEcole;

public sealed class UpdateEcoleCommandValidator : AbstractValidator<UpdateEcoleCommand>
{
    public UpdateEcoleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Nom).NotEmpty().MaximumLength(256);
        RuleFor(x => x.CodeEcole).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Type).NotEmpty().Must(t => Enum.TryParse<Domain.TypeEcole>(t, ignoreCase: true, out _))
            .WithMessage("Type doit être 'Public' ou 'Prive'.");
        RuleFor(x => x.Telephone).MaximumLength(20).When(x => x.Telephone is not null);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => x.Email is not null);
        RuleFor(x => x.Adresse).MaximumLength(500).When(x => x.Adresse is not null);
        RuleFor(x => x.Region).MaximumLength(100).When(x => x.Region is not null);
        RuleFor(x => x.Ville).MaximumLength(100).When(x => x.Ville is not null);
    }
}
