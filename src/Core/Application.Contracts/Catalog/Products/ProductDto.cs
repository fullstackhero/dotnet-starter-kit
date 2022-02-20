namespace FSH.WebApi.Application.Catalog.Products;

public class ProductDto : IDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Rate { get; set; }
    public string? ImagePath { get; set; }
    public Guid BrandId { get; set; }
    public string BrandName { get; set; } = default!;
}