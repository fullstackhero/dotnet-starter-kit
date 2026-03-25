using Mediator;

namespace FSH.Modules.SchoolManagement.Contracts.v1.Classes.UpdateClasse;

public sealed record UpdateClasseCommand(
    Guid Id,
    string Nom,
    string Niveau,
    int Capacite) : ICommand<Unit>;
