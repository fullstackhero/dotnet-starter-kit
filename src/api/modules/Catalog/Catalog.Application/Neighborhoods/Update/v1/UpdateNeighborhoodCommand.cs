using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Neighborhoods.Update.v1;

public sealed record UpdateNeighborhoodCommand(
    Guid Id,
    string? Name,
    string? Description,
    Guid? CityId,
    string? SphereImgURL,
    double? Score) : IRequest<UpdateNeighborhoodResponse>;