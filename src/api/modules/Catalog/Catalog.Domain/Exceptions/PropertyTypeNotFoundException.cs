using System;

namespace FSH.Starter.WebApi.Catalog.Domain.Exceptions;

public class PropertyTypeNotFoundException : Exception
{
    public PropertyTypeNotFoundException(Guid propertyTypeId)
        : base($"PropertyType with ID {propertyTypeId} was not found.")
    {
    }
}
