using Mediator;

namespace FSH.Modules.SchoolManagement.Contracts.v1.Classes.CreateClasse;

public sealed record CreateClasseCommand(
    string Nom,
    string Niveau,
    Guid EcoleId,
    Guid AnneeScolaireId,
    int Capacite) : ICommand<Guid>;
