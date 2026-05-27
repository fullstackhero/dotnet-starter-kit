using FSH.Modules.Catalog.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Brands;

public sealed record GetBrandByIdQuery(Guid BrandId) : IQuery<BrandDto>;
