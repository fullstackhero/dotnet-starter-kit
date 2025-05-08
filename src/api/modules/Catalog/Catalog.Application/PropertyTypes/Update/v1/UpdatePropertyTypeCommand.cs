using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.PropertyTypes.Update.v1;

public sealed record UpdatePropertyTypeCommand(
    Guid Id,
    string? Name,
    string? Description) : IRequest<UpdatePropertyTypeResponse>;
