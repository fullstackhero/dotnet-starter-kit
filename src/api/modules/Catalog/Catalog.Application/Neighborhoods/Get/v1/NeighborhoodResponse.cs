namespace FSH.Starter.WebApi.Catalog.Application.Neighborhoods.Get.v1;

public sealed record NeighborhoodResponse(Guid? Id, string Name, string Description, Guid CityId, string SphereImgURL, double Score);