using DN.WebApi.Shared.DTOs;

namespace DN.WebApi.Host.Endpoints.Catalog.Brands;

public class BrandDto : IDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}