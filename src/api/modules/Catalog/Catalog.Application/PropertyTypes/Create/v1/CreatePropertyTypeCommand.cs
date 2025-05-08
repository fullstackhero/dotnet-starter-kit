using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.PropertyTypes.Create.v1;

public sealed record CreatePropertyTypeCommand(
    string Name,
    string Description) : IRequest<CreatePropertyTypeResponse>;
