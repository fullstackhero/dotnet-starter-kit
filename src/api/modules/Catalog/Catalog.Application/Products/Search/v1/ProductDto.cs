namespace FSH.Starter.WebApi.Catalog.Application.Products.Search.v1;

public record ProductDto(Guid? Id, string Name, string? Description, decimal Price);
