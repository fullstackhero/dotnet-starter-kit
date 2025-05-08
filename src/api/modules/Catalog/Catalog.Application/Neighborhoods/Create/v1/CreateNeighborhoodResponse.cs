namespace FSH.Starter.WebApi.Catalog.Application.Neighborhoods.Create.v1;

public sealed record CreateNeighborhoodResponse(Guid? Id, string Name, string Description, Guid CityId, string SphereImgURL, double Score);