using FluentValidation;
using FSH.Modules.SchoolManagement.Contracts.v1.AnneeScolaires.CreateAnneeScolaire;

namespace FSH.Modules.SchoolManagement.Features.v1.AnneeScolaires.CreateAnneeScolaire;

public sealed class CreateAnneeScolaireCommandValidator : AbstractValidator<CreateAnneeScolaireCommand>
{
    public CreateAnneeScolaireCommandValidator()
    {
        RuleFor(x => x.Libelle).NotEmpty().MaximumLength(20);
        RuleFor(x => x.DateDebut).NotEmpty();
        RuleFor(x => x.DateFin).NotEmpty().GreaterThan(x => x.DateDebut)
            .WithMessage("End date must be after start date.");
    }
}
