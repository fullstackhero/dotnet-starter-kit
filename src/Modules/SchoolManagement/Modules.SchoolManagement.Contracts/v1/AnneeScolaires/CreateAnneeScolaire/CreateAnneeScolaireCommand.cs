using Mediator;

namespace FSH.Modules.SchoolManagement.Contracts.v1.AnneeScolaires.CreateAnneeScolaire;

public sealed record CreateAnneeScolaireCommand(
    string Libelle,
    DateTimeOffset DateDebut,
    DateTimeOffset DateFin) : ICommand<Guid>;
