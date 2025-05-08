using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.PropertyTypes.Delete.v1;

public sealed record DeletePropertyTypeCommand(Guid Id) : IRequest;
