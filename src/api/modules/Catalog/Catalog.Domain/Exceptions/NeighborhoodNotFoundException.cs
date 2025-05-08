using System;

namespace FSH.Starter.WebApi.Catalog.Domain.Exceptions;

public class NeighborhoodNotFoundException : Exception
{
    public NeighborhoodNotFoundException(Guid neighborhoodId)
        : base($"Neighborhood with ID {neighborhoodId} was not found.")
    {
    }
}
