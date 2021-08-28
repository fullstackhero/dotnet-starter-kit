using DN.WebApi.Application.Abstractions.Services.Catalog;
using DN.WebApi.Shared.DTOs.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Bootstrapper.Controllers.v1
{
    public class ProductsController : BaseController
    {
        private readonly IProductService _service;

        public ProductsController(IProductService service)
        {
            _service = service;
        }
        [HttpGet]
        public async Task<IActionResult> GetAsync(Guid id)
        {
            var productDetails = await _service.GetById(id);
            return Ok(productDetails);
        }
        [HttpPost]
        public async Task<IActionResult> CreateAsync(CreateProductRequest request)
        {
            return Ok(await _service.CreateProductAsync(request));
        }
    }
}