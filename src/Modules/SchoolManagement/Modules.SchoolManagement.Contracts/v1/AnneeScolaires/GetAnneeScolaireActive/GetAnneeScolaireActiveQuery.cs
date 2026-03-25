using FSH.Modules.SchoolManagement.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.SchoolManagement.Contracts.v1.AnneeScolaires.GetAnneeScolaireActive;

public sealed record GetAnneeScolaireActiveQuery : IQuery<AnneeScolaireDto?>;
