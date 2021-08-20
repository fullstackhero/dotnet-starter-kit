using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Bootstrapper.Controllers.Identity
{
    public class TokensController : BaseController
    {
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GenerateTokenAsync()
        {
            return Ok("token");
        }
    }
}