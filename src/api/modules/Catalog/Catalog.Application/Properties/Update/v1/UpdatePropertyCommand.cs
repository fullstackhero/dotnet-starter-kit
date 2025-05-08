using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Properties.Update.v1;

public sealed record UpdatePropertyCommand(
    Guid Id,
    string? Name,
    string? Description,
    Guid? NeighborhoodId,
    string? Address,
    decimal? AskingPrice,
    double? Size,
    int? Rooms,
    int? Bathrooms,
    Guid? PropertyTypeId,
    DateTime? ListedDate,
    DateTime? SoldDate,
    decimal? SoldPrice,
    string? FeatureList) : IRequest<UpdatePropertyResponse>;