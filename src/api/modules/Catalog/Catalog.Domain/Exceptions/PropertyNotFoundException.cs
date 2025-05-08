using System;

namespace FSH.Starter.WebApi.Catalog.Domain.Exceptions;

public class PropertyNotFoundException : Exception
{
    public PropertyNotFoundException(Guid propertyId)
        : base($"Property with ID {propertyId} was not found.")
    {
    }
}
