using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Regions.Delete.v1;

public sealed record DeleteRegionCommand(Guid Id) : IRequest;