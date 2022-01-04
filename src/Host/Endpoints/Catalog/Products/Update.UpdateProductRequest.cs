using DN.WebApi.Shared.DTOs.FileStorage;

namespace DN.WebApi.Host.Endpoints.Catalog.Products;

public class UpdateProductRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Rate { get; set; }
    public Guid BrandId { get; set; }
    public FileUploadRequest? Image { get; set; }
}