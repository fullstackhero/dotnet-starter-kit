using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Regions.Create.v1;

public sealed record CreateRegionCommand(
    string Name,
    string Description) : IRequest<CreateRegionResponse>;