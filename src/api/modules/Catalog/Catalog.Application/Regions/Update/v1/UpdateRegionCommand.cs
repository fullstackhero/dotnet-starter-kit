using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Regions.Update.v1;

public sealed record UpdateRegionCommand(
    Guid Id,
    string? Name,
    string? Description) : IRequest<UpdateRegionResponse>;
