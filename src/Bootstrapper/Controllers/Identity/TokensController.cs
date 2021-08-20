using DN.WebApi.Shared.DTOs.Identity.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Bootstrapper.Controllers.Identity
{
    public class TokensController : BaseController
    {
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> GenerateTokenAsync(TokenRequest request)
        {
            return Ok("token");
        }
    }
}