using FSH.Framework.Domain;

namespace FSH.WebApi.Modules.Catalog.Products.Models;
internal class Product : AuditableEntity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public string[] Tags { get; private set; } = Array.Empty<string>();

    public Product AddTag(string tag)
    {
        Tags ??= Array.Empty<string>();
        Tags = Tags.Append(tag).ToArray();
        return this;
    }

    public Product RemoveTag(string tag)
    {
        Tags ??= Array.Empty<string>();
        Tags = Tags.Where(x => x != tag).ToArray();
        return this;
    }

    public Product SetTags(string[] tags)
    {
        Tags = tags;
        return this;
    }
}
