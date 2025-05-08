namespace FSH.Starter.WebApi.Catalog.Application.Cities.Get.v1;

public sealed record CityResponse(Guid? Id, string Name, string Description, Guid RegionId);
