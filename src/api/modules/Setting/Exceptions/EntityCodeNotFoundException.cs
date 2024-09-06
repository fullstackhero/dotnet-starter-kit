using FSH.Framework.Core.Exceptions;

namespace FSH.Starter.WebApi.Setting.Exceptions;
internal sealed class EntityCodeNotFoundException : NotFoundException
{
    public EntityCodeNotFoundException(Guid id)
        : base($"EntityCode item with id {id} not found")
    {
    }
}
