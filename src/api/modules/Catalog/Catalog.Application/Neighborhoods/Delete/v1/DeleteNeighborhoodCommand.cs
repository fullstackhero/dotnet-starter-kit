using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Neighborhoods.Delete.v1;

public sealed record DeleteNeighborhoodCommand(Guid Id) : IRequest;
