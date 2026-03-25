using Mediator;

namespace FSH.Modules.SchoolManagement.Contracts.v1.AnneeScolaires.UpdateAnneeScolaire;

public sealed record UpdateAnneeScolaireCommand(
    Guid Id,
    string Libelle,
    DateTimeOffset DateDebut,
    DateTimeOffset DateFin,
    bool EstActive) : ICommand<Unit>;
