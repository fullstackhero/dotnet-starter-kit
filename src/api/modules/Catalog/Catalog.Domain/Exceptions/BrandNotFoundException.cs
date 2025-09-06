using FSH.Framework.Core.Exceptions;

namespace FSH.Starter.WebApi.Catalog.Domain.Exceptions;
public sealed class BrandNotFoundException : NotFoundException
{
    public BrandNotFoundException(Guid id)
        : base($"brand with id {id} not found")
    {
    }
}
