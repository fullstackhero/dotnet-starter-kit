using System;

namespace FSH.Starter.WebApi.Catalog.Domain.Exceptions;

public class AgencyNotFoundException : Exception
{
    public AgencyNotFoundException(Guid agencyId)
        : base($"Agency with ID {agencyId} was not found.")
    {
    }
}