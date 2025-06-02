using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Asp.Versioning;
using FSH.Framework.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FSH.Starter.WebApi.Host;

[ApiController]
[Route("api/v{version:apiVersion}/admin")]
[ApiVersion("1.0")]
[Authorize(Roles = "admin,customer_admin")] // Base authorization for all admin endpoints
public class AdminController : ControllerBase
{
    private readonly DapperUserRepository _userRepository;

    public AdminController(IDbConnection db)
    {
        _userRepository = new DapperUserRepository(db);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private List<string> GetCurrentUserRoles()
    {
        return HttpContext.User.Claims
            .Where(x => x.Type == ClaimTypes.Role)
            .Select(x => x.Value)
            .ToList();
    }

    private bool IsAdmin()
    {
        var roles = GetCurrentUserRoles();
        return roles.Contains(RoleConstants.Admin);
    }

    // USER MANAGEMENT ENDPOINTS

    [HttpGet("users")]
    [ProducesResponseType(typeof(List<UserDetail>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetUsersListAsync()
    {
        try
        {
            var users = await _userRepository.GetAllUsersAsync();
            var userDetails = new List<UserDetail>();

            foreach (var user in users)
            {
                var userRoles = await _userRepository.GetUserRolesAsync(user.id);
                userDetails.Add(new UserDetail
                {
                    Id = user.id,
                    Email = user.email,
                    Username = user.username,
                    FirstName = user.first_name,
                    LastName = user.last_name,
                    PhoneNumber = user.phone_number ?? string.Empty,
                    Profession = user.profession,
                    IsEmailVerified = user.is_email_verified,
                    IsPhoneVerified = user.is_phone_verified,
                    IsIdentityVerified = user.is_identity_verified,
                    EmailConfirmed = user.is_email_verified,
                    IsActive = user.status == "ACTIVE",
                    Roles = userRoles
                });
            }

            return Ok(userDetails);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get users error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("users")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> CreateUserAsync([FromBody] AdminCreateUserCommand request)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { Message = "User with this email already exists" });
            }

            // Check if username already exists
            if (await _userRepository.UsernameExistsAsync(request.Username))
            {
                return BadRequest(new { Message = "Username already exists" });
            }

            var user = new User
            {
                id = Guid.NewGuid(),
                email = request.Email,
                username = request.Username,
                phone_number = request.PhoneNumber,
                password_hash = request.Password, // This will be BCrypt hashed
                first_name = request.FirstName,
                last_name = request.LastName,
                profession = request.Profession,
                is_identity_verified = request.IsIdentityVerified ?? false,
                is_phone_verified = request.IsPhoneVerified ?? false,
                is_email_verified = request.IsEmailVerified ?? false,
                status = request.Status ?? "ACTIVE",
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            };

            await _userRepository.CreateAsync(user);
            
            // Assign role
            var roleToAssign = string.IsNullOrEmpty(request.Role) ? RoleConstants.BaseUser : request.Role;
            await _userRepository.AssignRoleToUserAsync(user.id, roleToAssign, GetCurrentUserId());
            
            return Ok(new { Message = "User created successfully", UserId = user.id });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Create user error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("users/{id}")]
    [Authorize(Roles = "admin,customer_admin,customer_support")]
    [ProducesResponseType(typeof(UserDetail), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetUserAsync(Guid id)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return BadRequest(new { Message = "User not found" });
            }

            var userRoles = await _userRepository.GetUserRolesAsync(user.id);

            var userDetail = new UserDetail
            {
                Id = user.id,
                Email = user.email,
                Username = user.username,
                FirstName = user.first_name,
                LastName = user.last_name,
                PhoneNumber = user.phone_number ?? string.Empty,
                Profession = user.profession,
                IsEmailVerified = user.is_email_verified,
                IsPhoneVerified = user.is_phone_verified,
                IsIdentityVerified = user.is_identity_verified,
                EmailConfirmed = user.is_email_verified,
                IsActive = user.status == "ACTIVE",
                Roles = userRoles
            };

            return Ok(userDetail);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get user error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPut("users/{id}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> UpdateUserAsync(Guid id, [FromBody] AdminUpdateUserCommand request)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return BadRequest(new { Message = "User not found" });
            }

            // Check if email is being changed and if it's available
            if (request.Email != user.email && await _userRepository.EmailExistsAsync(request.Email, id))
            {
                return BadRequest(new { Message = "Email already exists" });
            }

            // Check if username is being changed and if it's available
            if (request.Username != user.username && await _userRepository.UsernameExistsAsync(request.Username, id))
            {
                return BadRequest(new { Message = "Username already exists" });
            }

            user.email = request.Email;
            user.username = request.Username;
            user.first_name = request.FirstName;
            user.last_name = request.LastName;
            user.phone_number = request.PhoneNumber;
            user.profession = request.Profession;
            user.status = request.Status;
            user.is_identity_verified = request.IsIdentityVerified ?? false;
            user.is_phone_verified = request.IsPhoneVerified ?? false;
            user.is_email_verified = request.IsEmailVerified ?? false;
            user.updated_at = DateTime.UtcNow;

            await _userRepository.AdminUpdateUserAsync(user);
            return Ok(new { Message = "User updated successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Update user error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpDelete("users/{id}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> DeleteUserAsync(Guid id)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return BadRequest(new { Message = "User not found" });
            }

            // Soft delete by default
            await _userRepository.DeleteUserAsync(id);
            return Ok(new { Message = "User deleted successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Delete user error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpDelete("users/{id}/permanent")]
    [Authorize(Roles = "admin")] // Only super admin
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> PermanentDeleteUserAsync(Guid id)
    {
        try
        {
            if (!IsAdmin())
            {
                return Forbid("Only administrators can permanently delete users");
            }

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return BadRequest(new { Message = "User not found" });
            }

            await _userRepository.HardDeleteUserAsync(id);
            return Ok(new { Message = "User permanently deleted" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Permanent delete user error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    // ROLE MANAGEMENT ENDPOINTS

    [HttpGet("users/{id}/roles")]
    [Authorize(Roles = "admin,customer_admin,customer_support")]
    [ProducesResponseType(typeof(List<string>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetUserRolesAsync(Guid id)
    {
        try
        {
            var roles = await _userRepository.GetUserRolesAsync(id);
            return Ok(roles);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get user roles error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("users/{id}/roles")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> AssignRoleAsync(Guid id, [FromBody] AssignRoleCommand request)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return BadRequest(new { Message = "User not found" });
            }

            // Check if role is valid
            if (!RoleConstants.AllRoles.Contains(request.RoleId))
            {
                return BadRequest(new { Message = "Invalid role" });
            }

            await _userRepository.AssignRoleToUserAsync(id, request.RoleId, GetCurrentUserId());
            return Ok(new { Message = "Role assigned successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Assign role error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpDelete("users/{id}/roles/{roleId}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> RemoveRoleAsync(Guid id, string roleId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return BadRequest(new { Message = "User not found" });
            }

            await _userRepository.RemoveRoleFromUserAsync(id, roleId);
            return Ok(new { Message = "Role removed successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Remove role error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("roles")]
    [ProducesResponseType(typeof(List<RoleDto>), 200)]
    [ProducesResponseType(400)]
    public IActionResult GetRoles()
    {
        try
        {
            var roles = new List<RoleDto>
            {
                new RoleDto { Id = RoleConstants.Admin, Name = "Administrator", Description = "Full system access" },
                new RoleDto { Id = RoleConstants.CustomerAdmin, Name = "Customer Administrator", Description = "Customer organization admin" },
                new RoleDto { Id = RoleConstants.CustomerSupport, Name = "Customer Support", Description = "Customer support access" },
                new RoleDto { Id = RoleConstants.BaseUser, Name = "Base User", Description = "Standard user access" }
            };
            return Ok(roles);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get roles error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("users/by-role/{roleId}")]
    [ProducesResponseType(typeof(List<UserDetail>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetUsersByRoleAsync(string roleId)
    {
        try
        {
            if (!RoleConstants.AllRoles.Contains(roleId))
            {
                return BadRequest(new { Message = "Invalid role" });
            }

            var users = await _userRepository.GetUsersByRoleAsync(roleId);
            var userDetails = users.Select(user => new UserDetail
            {
                Id = user.id,
                Email = user.email,
                Username = user.username,
                FirstName = user.first_name,
                LastName = user.last_name,
                PhoneNumber = user.phone_number ?? string.Empty,
                Profession = user.profession,
                IsEmailVerified = user.is_email_verified,
                IsPhoneVerified = user.is_phone_verified,
                IsIdentityVerified = user.is_identity_verified,
                EmailConfirmed = user.is_email_verified,
                IsActive = user.status == "ACTIVE"
            }).ToList();

            return Ok(userDetails);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get users by role error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    // BOOTSTRAP ENDPOINT (Development Only)
    [HttpPost("bootstrap/assign-admin/{userId}")]
    [AllowAnonymous] // WARNING: Remove this endpoint in production!
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> BootstrapAssignAdminAsync(Guid userId)
    {
        try
        {
            // PRODUCTION SECURITY CHECK
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            if (!isDevelopment)
            {
                return BadRequest(new { Message = "Bootstrap endpoint is disabled in production for security reasons." });
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return BadRequest(new { Message = "User not found" });
            }

            // Check if user already has admin role
            var hasAdminRole = await _userRepository.UserHasRoleAsync(userId, "admin");
            if (hasAdminRole)
            {
                return Ok(new { Message = "User already has admin role" });
            }

            await _userRepository.AssignRoleToUserAsync(userId, "admin");
            return Ok(new { Message = "Admin role assigned successfully for bootstrap" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Bootstrap assign admin error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }
}

// ADMIN COMMAND MODELS
public class AdminCreateUserCommand
{
    public string Email { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? Profession { get; set; }
    public string? Role { get; set; } = RoleConstants.BaseUser;
    public string? Status { get; set; } = "ACTIVE";
    public bool? IsIdentityVerified { get; set; }
    public bool? IsPhoneVerified { get; set; }
    public bool? IsEmailVerified { get; set; }
}

public class AdminUpdateUserCommand
{
    public string Email { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string? Profession { get; set; }
    public string Status { get; set; } = default!;
    public bool? IsIdentityVerified { get; set; }
    public bool? IsPhoneVerified { get; set; }
    public bool? IsEmailVerified { get; set; }
} 