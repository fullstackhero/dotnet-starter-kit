using FSH.Framework.Core.Exceptions;

namespace EntityCode.Exceptions;
internal sealed class EntityCodeNotFoundException : NotFoundException
{
    public EntityCodeNotFoundException(Guid id)
        : base($"EntityCode item with id {id} not found")
    {
    }
}
