using FSH.Modules.Multitenancy.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Multitenancy.Contracts.v1.GetTenantStatus;

public sealed record GetTenantStatusQuery(string TenantId) : IQuery<TenantStatusDto>;