using FSH.Framework.Core.Domain;

namespace FSH.WebApi.Catalog.Domain;
public class Product : AuditableEntity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
}
