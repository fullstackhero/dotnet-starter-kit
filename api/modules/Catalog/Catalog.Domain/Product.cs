using FSH.Framework.Domain;

namespace FSH.WebApi.Modules.Catalog.Features.Products;
public class Product : AuditableEntity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
}
