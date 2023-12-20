using MediatR;

namespace FSH.WebApi.Catalog.Application.Products.Creation.v1;
public sealed record ProductCreationCommand(string? Name, decimal Price, string? Description = null) : IRequest<ProductCreationResponse>;
