using FSH.Modules.SchoolManagement.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.SchoolManagement.Contracts.v1.AnneeScolaires.GetAnneeScolaires;

public sealed record GetAnneeScolairesQuery : IQuery<IReadOnlyCollection<AnneeScolaireDto>>;
