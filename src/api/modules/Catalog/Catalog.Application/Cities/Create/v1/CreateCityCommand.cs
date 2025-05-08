using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Cities.Create.v1;

public sealed record CreateCityCommand(
    string Name,
    string Description,
    Guid RegionId) : IRequest<CreateCityResponse>;
