using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DN.WebApi.Application.Abstractions.Services.Identity;
using DN.WebApi.Shared.DTOs.Identity.Requests;
using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Bootstrapper.Controllers.Identity
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync(string id)
        {
            var user = await _userService.GetAsync(id);
            return Ok(user);
        }

        [HttpGet("{id}/roles")]
        public async Task<IActionResult> GetRolesAsync(string id)
        {
            var userRoles = await _userService.GetRolesAsync(id);
            return Ok(userRoles);
        }

        [HttpPost("{id}/roles")]
        public async Task<IActionResult> AssignRolesAsync(string id, UserRolesRequest request)
        {
            var result = await _userService.AssignRolesAsync(id, request);
            return Ok(result);
        }
    }
}