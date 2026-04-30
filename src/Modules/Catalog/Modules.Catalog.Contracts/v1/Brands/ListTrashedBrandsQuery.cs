using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Brands;

/// <summary>
/// Lists soft-deleted brands. Bypasses the global IsDeleted query filter.
/// </summary>
public sealed record ListTrashedBrandsQuery(int PageNumber = 1, int PageSize = 20)
    : IQuery<PagedResponse<BrandDto>>;
