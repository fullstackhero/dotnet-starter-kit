namespace FSH.Starter.WebApi.Catalog.Application.Agencies.Create.v1;

public sealed record CreateAgencyResponse(Guid? Id, string Name, string Email, string Telephone, string Adress, string Description);
