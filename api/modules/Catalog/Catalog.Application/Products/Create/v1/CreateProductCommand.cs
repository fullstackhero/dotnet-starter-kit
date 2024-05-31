using System.ComponentModel;
using MediatR;

namespace FSH.WebApi.Catalog.Application.Products.Creation.v1;
public sealed record CreateProductCommand(
    [property: DefaultValue("Sample Product")] string? Name,
    [property: DefaultValue(10)] decimal Price,
    [property: DefaultValue("Descriptive Description")] string? Description = null) : IRequest<CreateProductResponse>;
