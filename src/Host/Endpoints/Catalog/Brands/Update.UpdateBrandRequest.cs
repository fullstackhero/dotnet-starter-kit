namespace DN.WebApi.Host.Endpoints.Catalog.Brands;

public class UpdateBrandRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}