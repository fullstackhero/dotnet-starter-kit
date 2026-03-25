using FluentValidation;
using FSH.Modules.SchoolManagement.Contracts.v1.Classes.UpdateClasse;

namespace FSH.Modules.SchoolManagement.Features.v1.Classes.UpdateClasse;

public sealed class UpdateClasseCommandValidator : AbstractValidator<UpdateClasseCommand>
{
    public UpdateClasseCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Nom).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Niveau).NotEmpty().Must(n => Enum.TryParse<Domain.NiveauScolaire>(n, ignoreCase: true, out _))
            .WithMessage("Invalid school level.");
        RuleFor(x => x.Capacite).GreaterThan(0);
    }
}
