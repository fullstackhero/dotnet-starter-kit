using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Cities.Delete.v1;

public sealed record DeleteCityCommand(Guid Id) : IRequest;
