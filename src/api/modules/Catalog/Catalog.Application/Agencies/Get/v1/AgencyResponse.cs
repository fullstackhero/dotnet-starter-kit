namespace FSH.Starter.WebApi.Catalog.Application.Agencies.Get.v1;
public sealed record AgencyResponse(Guid? Id, string Name, string Email, string Telephone, string Address, string Description);
