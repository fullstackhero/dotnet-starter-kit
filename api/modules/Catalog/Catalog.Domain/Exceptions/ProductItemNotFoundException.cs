using FSH.Framework.Core.Exceptions;

namespace FSH.WebApi.Catalog.Domain.Exceptions;
public sealed class ProductItemNotFoundException : NotFoundException
{
    public ProductItemNotFoundException(Guid id)
        : base($"product with id {id} not found")
    {
    }
}
