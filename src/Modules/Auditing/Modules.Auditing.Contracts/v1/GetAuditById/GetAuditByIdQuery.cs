using FSH.Modules.Auditing.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Auditing.Contracts.v1.GetAuditById;

public sealed record GetAuditByIdQuery(Guid Id) : IQuery<AuditDetailDto>;