using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Properties.Delete.v1;

public sealed record DeletePropertyCommand(Guid Id) : IRequest;