using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Host.Endpoints.Catalog.Brands;

public class DeleteBrandRequest
{
    [FromRoute(Name = "id")]
    public Guid Id { get; set; }
}