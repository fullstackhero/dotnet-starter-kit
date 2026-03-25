using FSH.Modules.SchoolManagement.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.SchoolManagement.Contracts.v1.Ecoles.GetEcoleById;

public sealed record GetEcoleByIdQuery(Guid Id) : IQuery<EcoleDto>;
