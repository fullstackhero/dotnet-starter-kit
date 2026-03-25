using FluentValidation;
using FSH.Modules.SchoolManagement.Contracts.v1.Matieres.CreateMatiere;

namespace FSH.Modules.SchoolManagement.Features.v1.Matieres.CreateMatiere;

public sealed class CreateMatiereCommandValidator : AbstractValidator<CreateMatiereCommand>
{
    public CreateMatiereCommandValidator()
    {
        RuleFor(x => x.Nom).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Coefficient).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
    }
}
