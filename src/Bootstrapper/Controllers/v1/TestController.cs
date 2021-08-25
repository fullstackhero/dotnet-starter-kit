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
        private readonly ICurrentUser _currentUser;

        public TestController(ICurrentUser currentUser, ApplicationDbContext context)
        {
            _currentUser = currentUser;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> TestAsync()
        {
            var tenant = _currentUser.GetTenantId();
            var users = await _context.Users.Where(a => a.TenantId == tenant).ToListAsync();
            return Ok(users);
        }
    }
}