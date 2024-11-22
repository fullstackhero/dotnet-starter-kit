using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Brands.Update.v1;
public sealed record UpdateBrandCommand(
    Guid Id,
    string? Name,
    string? Description = null) : IRequest<UpdateBrandResponse>;
