using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Cities.Update.v1;

public sealed record UpdateCityCommand(
    Guid Id,
    string? Name,
    string? Description,
    Guid? RegionId) : IRequest<UpdateCityResponse>;
