using System;

namespace FSH.Starter.WebApi.Catalog.Domain.Exceptions;

public class CityNotFoundException : Exception
{
    public CityNotFoundException(Guid cityId)
        : base($"City with ID {cityId} was not found.")
    {
    }
}
