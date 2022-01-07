using DN.WebApi.Application.Catalog.Brands;
using DN.WebApi.Shared.DTOs;

namespace DN.WebApi.Application.Catalog.Products;

public class ProductDetailsDto : IDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal Rate { get; set; }
    public string? ImagePath { get; set; }
    public BrandDto? Brand { get; set; }
}