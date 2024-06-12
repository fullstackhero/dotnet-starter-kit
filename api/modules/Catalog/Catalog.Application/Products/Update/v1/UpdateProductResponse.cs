namespace FSH.WebApi.Catalog.Application.Products.Update.v1;
public sealed record UpdateProductResponse(Guid? Id, string Name, string Description, decimal Price);
