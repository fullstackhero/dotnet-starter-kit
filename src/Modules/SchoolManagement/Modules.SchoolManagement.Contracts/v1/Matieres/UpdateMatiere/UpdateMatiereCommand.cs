using Mediator;

namespace FSH.Modules.SchoolManagement.Contracts.v1.Matieres.UpdateMatiere;

public sealed record UpdateMatiereCommand(
    Guid Id,
    string Nom,
    string Code,
    int Coefficient,
    string? Description) : ICommand<Unit>;
