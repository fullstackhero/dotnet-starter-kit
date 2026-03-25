using FluentValidation;
using FSH.Modules.SchoolManagement.Contracts.v1.Matieres.UpdateMatiere;

namespace FSH.Modules.SchoolManagement.Features.v1.Matieres.UpdateMatiere;

public sealed class UpdateMatiereCommandValidator : AbstractValidator<UpdateMatiereCommand>
{
    public UpdateMatiereCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Nom).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Coefficient).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
    }
}
