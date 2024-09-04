using FSH.Framework.Core.Exceptions;

namespace FSH.Starter.WebApi.Setting.Dimension.Exceptions;
internal sealed class DimensionNotFoundException : NotFoundException
{
    public DimensionNotFoundException(Guid id)
        : base($"Dimension item with id {id} not found")
    {
    }
}
