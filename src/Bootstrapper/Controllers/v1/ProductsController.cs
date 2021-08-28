using DN.WebApi.Application.Abstractions.Services.Catalog;
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
        public async Task<IActionResult> Get(Guid id)
        {
            var productDetails = await _service.GetById(id);
            return Ok(productDetails);
        }
    }
}