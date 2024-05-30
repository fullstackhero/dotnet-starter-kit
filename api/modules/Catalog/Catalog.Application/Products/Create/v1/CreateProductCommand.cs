using MediatR;

namespace FSH.WebApi.Catalog.Application.Products.Creation.v1;
public sealed record CreateProductCommand(string? Name, decimal Price, string? Description = null) : IRequest<CreateProductResponse>;
