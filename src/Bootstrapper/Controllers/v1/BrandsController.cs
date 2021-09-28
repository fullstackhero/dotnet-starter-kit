using System.Threading.Tasks;
using DN.WebApi.Application.Abstractions.Services.Catalog;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Identity.Permissions;
using DN.WebApi.Shared.DTOs.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Bootstrapper.Controllers.v1
{
    public class BrandsController : BaseController
    {
        private readonly IBrandService _service;

        public BrandsController(IBrandService service)
        {
            _service = service;
        }

        [HttpGet]
        [MustHavePermission(Permissions.Brands.ListAll)]
        public async Task<IActionResult> GetListAsync([FromQuery]BrandListFilter filter)
        {
            var productDetails = await _service.GetBrandsAsync(filter);
            return Ok(productDetails);
        }

        [HttpPost]
        [MustHavePermission(Permissions.Brands.Register)]
        public async Task<IActionResult> CreateAsync(CreateBrandRequest request)
        {
            return Ok(await _service.CreateBrandAsync(request));
        }
    }
}