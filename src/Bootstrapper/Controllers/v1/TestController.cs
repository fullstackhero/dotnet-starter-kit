using DN.WebApi.Application.Abstractions.Services.Identity;
using DN.WebApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DN.WebApi.Bootstrapper.Controllers.v1
{
    [Authorize]
    public class TestController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public TestController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> TestAsync()
        {
            var users = await _context.Users.ToListAsync();
            return Ok(users);
        }
    }
}