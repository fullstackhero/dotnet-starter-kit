namespace FSH.WebApi.Application.Catalog.Brands;

public class BrandDto : IDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}