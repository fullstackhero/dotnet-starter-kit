namespace FSH.WebApi.Catalog.Application.Products.Get.v1;
public sealed record GetProductResponse(Guid? Id, string Name, string? Description, decimal Price);
