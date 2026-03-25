using FSH.Modules.SchoolManagement.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.SchoolManagement.Contracts.v1.Matieres.GetMatieres;

public sealed record GetMatieresQuery(string? Search = null) : IQuery<IReadOnlyCollection<MatiereDto>>;
