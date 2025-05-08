using System;

namespace FSH.Starter.WebApi.Catalog.Domain.Exceptions;

public class RegionNotFoundException : Exception
{
    public RegionNotFoundException(Guid regionId)
        : base($"Region with ID {regionId} was not found.")
    {
    }
}
