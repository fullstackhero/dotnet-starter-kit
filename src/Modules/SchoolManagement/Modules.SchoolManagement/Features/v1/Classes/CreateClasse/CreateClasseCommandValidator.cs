using FluentValidation;
using FSH.Modules.SchoolManagement.Contracts.v1.Classes.CreateClasse;

namespace FSH.Modules.SchoolManagement.Features.v1.Classes.CreateClasse;

public sealed class CreateClasseCommandValidator : AbstractValidator<CreateClasseCommand>
{
    public CreateClasseCommandValidator()
    {
        RuleFor(x => x.Nom).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Niveau).NotEmpty().Must(n => Enum.TryParse<Domain.NiveauScolaire>(n, ignoreCase: true, out _))
            .WithMessage("Invalid school level.");
        RuleFor(x => x.EcoleId).NotEmpty();
        RuleFor(x => x.AnneeScolaireId).NotEmpty();
        RuleFor(x => x.Capacite).GreaterThan(0);
    }
}
