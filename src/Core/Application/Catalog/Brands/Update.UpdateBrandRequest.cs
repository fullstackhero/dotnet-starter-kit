namespace DN.WebApi.Application.Catalog.Brands;

public class UpdateBrandRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}