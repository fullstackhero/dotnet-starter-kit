namespace FL_CRMS_ERP_WEBAPI.Application.Catalog.Brands;

public class BrandDto : IDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}