using Mediator;

namespace FSH.Modules.SchoolManagement.Contracts.v1.Matieres.CreateMatiere;

public sealed record CreateMatiereCommand(
    string Nom,
    string Code,
    int Coefficient,
    string? Description) : ICommand<Guid>;
