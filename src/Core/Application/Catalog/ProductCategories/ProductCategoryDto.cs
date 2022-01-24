namespace FSH.WebApi.Application.Catalog.Products;

public class ProductCategoryDto : IDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Icon { get; set; }
}