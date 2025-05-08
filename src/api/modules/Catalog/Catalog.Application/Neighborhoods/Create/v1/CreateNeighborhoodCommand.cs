using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Neighborhoods.Create.v1;

public sealed record CreateNeighborhoodCommand(
    string Name,
    string Description,
    Guid CityId,
    string SphereImgURL,
    double Score) : IRequest<CreateNeighborhoodResponse>;
