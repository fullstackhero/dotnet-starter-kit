using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Asp.Versioning;
using FSH.Framework.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FSH.Starter.WebApi.Host;

[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[ApiVersion("1.0")]
public class AuthController : ControllerBase
{
    private readonly DapperUserRepository _userRepository;
    private readonly string _jwtSecret;

    public AuthController(IDbConnection db, IConfiguration config)
    {
        try
        {
            Console.WriteLine("AuthController constructor started");
            
            _userRepository = new DapperUserRepository(db);
            Console.WriteLine("DapperUserRepository created successfully");
            
            // Debug logging
            var jwtKey = config["JwtOptions:Key"];
            Console.WriteLine($"JWT Key from config: {jwtKey}");
            Console.WriteLine($"JWT Key is null or empty: {string.IsNullOrEmpty(jwtKey)}");
            
            _jwtSecret = jwtKey ?? throw new InvalidOperationException("JwtOptions:Key is not configured");
            
            Console.WriteLine($"JWT Secret set successfully: {!string.IsNullOrEmpty(_jwtSecret)}");
            Console.WriteLine("AuthController constructor completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AuthController constructor error: {ex.Message}");
            Console.WriteLine($"AuthController constructor stack trace: {ex.StackTrace}");
            throw;
        }
    }

    [HttpGet("test")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult Test()
    {
        return Ok(new { Message = "AuthController is working", JwtSecretExists = !string.IsNullOrEmpty(_jwtSecret) });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request)
    {
        try
        {
            Console.WriteLine($"Login attempt for email: {request.Email}");
            
            var user = await _userRepository.GetByEmailAsync(request.Email);
            Console.WriteLine($"User found: {user != null}");
            
            if (user != null)
            {
                Console.WriteLine($"User ID: {user.id}");
                Console.WriteLine($"User Email: {user.email}");
                Console.WriteLine($"Password Hash: {user.password_hash}");
                Console.WriteLine($"Password Hash is null or empty: {string.IsNullOrEmpty(user.password_hash)}");
            }
            
            if (user == null || !await _userRepository.ValidatePasswordAsync(request.Email, request.Password))
            {
                Console.WriteLine("Authentication failed - user not found or invalid password");
                return Unauthorized();
            }

            // Debug logging
            Console.WriteLine($"User: {user.email}, FirstName: {user.first_name}, LastName: {user.last_name}");
            Console.WriteLine($"JWT Secret: {_jwtSecret}");

            var token = JwtHelper.GenerateToken(user, _jwtSecret);
            return Ok(new { Token = token });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest request)
    {
        // Check if user already exists
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest(new { Message = "User with this email already exists" });
        }

        var user = new User
        {
            id = Guid.NewGuid(),
            email = request.Email,
            phone_number = request.PhoneNumber,
            tckn = request.Tckn,
            password_hash = request.Password, // Bu BCrypt ile hash'lenecek
            first_name = request.FirstName,
            last_name = request.LastName,
            birth_date = request.BirthDate ?? DateTime.UtcNow.AddYears(-25), // Default if null
            is_identity_verified = false,
            is_phone_verified = false,
            is_email_verified = false,
            status = "ACTIVE",
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(user);
        return Ok(new { Message = "User created successfully", UserId = user.id });
    }

    [HttpPost("token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GenerateTokenAsync([FromBody] TokenGenerationCommand request)
    {
        try
        {
            Console.WriteLine($"Token generation attempt for email: {request.Email}");
            
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null || !await _userRepository.ValidatePasswordAsync(request.Email, request.Password))
            {
                Console.WriteLine("Authentication failed - user not found or invalid password");
                return Unauthorized();
            }

            var token = JwtHelper.GenerateToken(user, _jwtSecret);
            var refreshToken = Guid.NewGuid().ToString(); // Simple refresh token for now
            
            return Ok(new TokenResponse 
            { 
                Token = token, 
                RefreshToken = refreshToken,
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Token generation error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(400)]
    public Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenCommand request)
    {
        try
        {
            // For now, just validate that refresh token exists and generate new token
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return Task.FromResult<IActionResult>(Unauthorized(new { Message = "Invalid refresh token" }));
            }

            // In a real implementation, you'd validate the refresh token against database
            // For now, we'll just generate a new token set
            var newToken = request.Token; // Simplified for now
            var newRefreshToken = Guid.NewGuid().ToString();
            
            return Task.FromResult<IActionResult>(Ok(new TokenResponse 
            { 
                Token = newToken, 
                RefreshToken = newRefreshToken,
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
            }));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Token refresh error: {ex.Message}");
            return Task.FromResult<IActionResult>(BadRequest(new { Error = ex.Message }));
        }
    }

    [HttpGet("permissions")]
    [Authorize]
    [ProducesResponseType(typeof(List<string>), 200)]
    [ProducesResponseType(400)]
    public Task<IActionResult> GetUserPermissionsAsync()
    {
        try
        {
            // For now, return basic permissions
            // In a real implementation, you'd get user-specific permissions from database
            var permissions = new List<string>
            {
                "Permissions.Users.View",
                "Permissions.Users.Create",
                "Permissions.Dashboard.View"
            };
            
            return Task.FromResult<IActionResult>(Ok(permissions));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get permissions error: {ex.Message}");
            return Task.FromResult<IActionResult>(BadRequest(new { Error = ex.Message }));
        }
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordCommand request)
    {
        try
        {
            // In a real implementation, you'd:
            // 1. Check if user exists with _userRepository.GetByEmailAsync(request.Email)
            // 2. Generate a password reset token
            // 3. Save it to database with expiry
            // 4. Send email with reset link
            Console.WriteLine($"Password reset requested for: {request.Email}");
            return Task.FromResult<IActionResult>(Ok(new { Message = "If an account with that email exists, a password reset link has been sent." }));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Forgot password error: {ex.Message}");
            return Task.FromResult<IActionResult>(BadRequest(new { Error = ex.Message }));
        }
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordCommand request)
    {
        try
        {
            // Get current user from token
            var currentUserEmail = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "email")?.Value;
            if (string.IsNullOrEmpty(currentUserEmail))
            {
                return BadRequest(new { Message = "Unable to determine current user" });
            }

            // Validate current password
            if (!await _userRepository.ValidatePasswordAsync(currentUserEmail, request.CurrentPassword))
            {
                return BadRequest(new { Message = "Current password is incorrect" });
            }

            var user = await _userRepository.GetByEmailAsync(currentUserEmail);
            if (user == null)
            {
                return BadRequest(new { Message = "User not found" });
            }

            await _userRepository.UpdatePasswordAsync(user.id, request.NewPassword);
            return Ok(new { Message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Change password error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserDetail), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetProfileAsync()
    {
        try
        {
            var currentUserEmail = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "email")?.Value;
            if (string.IsNullOrEmpty(currentUserEmail))
            {
                return BadRequest(new { Message = "Unable to determine current user" });
            }

            var user = await _userRepository.GetByEmailAsync(currentUserEmail);
            if (user == null)
            {
                return BadRequest(new { Message = "User not found" });
            }

            var userDetail = new UserDetail
            {
                Id = user.id,
                Email = user.email,
                FirstName = user.first_name,
                LastName = user.last_name,
                PhoneNumber = user.phone_number ?? string.Empty,
                IsEmailVerified = user.is_email_verified,
                IsPhoneVerified = user.is_phone_verified,
                IsIdentityVerified = user.is_identity_verified,
                UserName = user.email, // Using email as username for now
                EmailConfirmed = user.is_email_verified,
                IsActive = user.status == "ACTIVE"
            };

            return Ok(userDetail);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get profile error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdateProfileAsync([FromBody] UpdateUserCommand request)
    {
        try
        {
            var currentUserEmail = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "email")?.Value;
            if (string.IsNullOrEmpty(currentUserEmail))
            {
                return BadRequest(new { Message = "Unable to determine current user" });
            }

            var user = await _userRepository.GetByEmailAsync(currentUserEmail);
            if (user == null)
            {
                return BadRequest(new { Message = "User not found" });
            }

            user.first_name = request.FirstName;
            user.last_name = request.LastName;
            user.phone_number = request.PhoneNumber;
            user.updated_at = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            return Ok(new { Message = "Profile updated successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Update profile error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("users")]
    [Authorize]
    [ProducesResponseType(typeof(List<UserDetail>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetUsersListAsync()
    {
        try
        {
            var users = await _userRepository.GetAllUsersAsync();
            var userDetails = users.Select(user => new UserDetail
            {
                Id = user.id,
                Email = user.email,
                FirstName = user.first_name,
                LastName = user.last_name,
                PhoneNumber = user.phone_number ?? string.Empty,
                IsEmailVerified = user.is_email_verified,
                IsPhoneVerified = user.is_phone_verified,
                IsIdentityVerified = user.is_identity_verified,
                UserName = user.email,
                EmailConfirmed = user.is_email_verified,
                IsActive = user.status == "ACTIVE"
            }).ToList();

            return Ok(userDetails);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get users error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("users/register")]
    [Authorize]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> RegisterUserAsync([FromBody] RegisterUserCommand request)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { Message = "User with this email already exists" });
            }

            var user = new User
            {
                id = Guid.NewGuid(),
                email = request.Email,
                phone_number = request.PhoneNumber,
                password_hash = request.Password, // This will be BCrypt hashed
                first_name = request.FirstName,
                last_name = request.LastName,
                is_identity_verified = false,
                is_phone_verified = false,
                is_email_verified = false,
                status = "ACTIVE",
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            };

            await _userRepository.CreateAsync(user);
            return Ok(new { Message = "User created successfully", UserId = user.id });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Register user error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("users/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(UserDetail), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetUserAsync(Guid id)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return BadRequest(new { Message = "User not found" });
            }

            var userDetail = new UserDetail
            {
                Id = user.id,
                Email = user.email,
                FirstName = user.first_name,
                LastName = user.last_name,
                PhoneNumber = user.phone_number ?? string.Empty,
                IsEmailVerified = user.is_email_verified,
                IsPhoneVerified = user.is_phone_verified,
                IsIdentityVerified = user.is_identity_verified,
                UserName = user.email,
                EmailConfirmed = user.is_email_verified,
                IsActive = user.status == "ACTIVE"
            };

            return Ok(userDetail);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get user error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("roles")]
    [Authorize]
    [ProducesResponseType(typeof(List<RoleDto>), 200)]
    [ProducesResponseType(400)]
    public Task<IActionResult> GetRolesAsync()
    {
        try
        {
            // For now, return predefined roles
            // In a real implementation, you'd get roles from database
            var roles = new List<RoleDto>
            {
                new RoleDto { Id = "admin", Name = "Administrator", Description = "Full system access", Permissions = { "Permissions.All" } },
                new RoleDto { Id = "user", Name = "User", Description = "Standard user access", Permissions = { "Permissions.Users.View" } },
                new RoleDto { Id = "moderator", Name = "Moderator", Description = "Moderate user access", Permissions = { "Permissions.Users.View", "Permissions.Users.Create" } }
            };
            
            return Task.FromResult<IActionResult>(Ok(roles));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get roles error: {ex.Message}");
            return Task.FromResult<IActionResult>(BadRequest(new { Error = ex.Message }));
        }
    }

    [HttpPost("roles")]
    [Authorize]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public Task<IActionResult> CreateOrUpdateRoleAsync([FromBody] CreateOrUpdateRoleCommand request)
    {
        try
        {
            // For now, just return success
            // In a real implementation, you'd save to database
            return Task.FromResult<IActionResult>(Ok(new { Message = "Role created/updated successfully", RoleId = request.Id }));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Create/Update role error: {ex.Message}");
            return Task.FromResult<IActionResult>(BadRequest(new { Error = ex.Message }));
        }
    }

    [HttpDelete("roles/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public Task<IActionResult> DeleteRoleAsync(string id)
    {
        try
        {
            // For now, just return success
            // In a real implementation, you'd delete from database
            if (string.IsNullOrEmpty(id))
            {
                return Task.FromResult<IActionResult>(BadRequest(new { Message = "Role ID is required" }));
            }

            return Task.FromResult<IActionResult>(Ok(new { Message = "Role deleted successfully" }));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Delete role error: {ex.Message}");
            return Task.FromResult<IActionResult>(BadRequest(new { Error = ex.Message }));
        }
    }

    [HttpGet("users/{id}/roles")]
    [Authorize]
    [ProducesResponseType(typeof(List<UserRoleDetail>), 200)]
    [ProducesResponseType(400)]
    public Task<IActionResult> GetUserRolesAsync(Guid id)
    {
        try
        {
            // For now, return predefined user roles. In a real implementation, you'd get from database
            var userRoles = new List<UserRoleDetail>
            {
                new UserRoleDetail { RoleId = "user", RoleName = "User", Description = "Standard user access", Enabled = true }
            };
            
            return Task.FromResult<IActionResult>(Ok(userRoles));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get user roles error: {ex.Message}");
            return Task.FromResult<IActionResult>(BadRequest(new { Error = ex.Message }));
        }
    }

    [HttpPost("users/{id}/roles")]
    [Authorize]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> AssignRolesToUserAsync(Guid id, [FromBody] AssignUserRoleCommand request)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return BadRequest(new { Message = "User not found" });
            }

            // For now, just return success. In a real implementation, you'd save role assignments to database
            return Ok(new { Message = "Roles assigned successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Assign roles error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("users/{id}/audit-trails")]
    [Authorize]
    [ProducesResponseType(typeof(List<AuditTrail>), 200)]
    [ProducesResponseType(400)]
    public Task<IActionResult> GetUserAuditTrailsAsync(Guid id)
    {
        try
        {
            // For now, return empty audit trails. In a real implementation, you'd get from database
            var auditTrails = new List<AuditTrail>();
            
            return Task.FromResult<IActionResult>(Ok(auditTrails));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get audit trails error: {ex.Message}");
            return Task.FromResult<IActionResult>(BadRequest(new { Error = ex.Message }));
        }
    }

    [HttpGet("roles/{id}/permissions")]
    [Authorize]
    [ProducesResponseType(typeof(RoleDto), 200)]
    [ProducesResponseType(400)]
    public Task<IActionResult> GetRolePermissionsAsync(string id)
    {
        try
        {
            // For now, return predefined role permissions
            // In a real implementation, you'd get from database
            var role = id.ToUpperInvariant() switch
            {
                "ADMIN" => new RoleDto 
                { 
                    Id = "admin", 
                    Name = "Administrator", 
                    Description = "Full system access", 
                    Permissions = { "Permissions.All" } 
                },
                "USER" => new RoleDto 
                { 
                    Id = "user", 
                    Name = "User", 
                    Description = "Standard user access", 
                    Permissions = { "Permissions.Users.View" } 
                },
                "MODERATOR" => new RoleDto 
                { 
                    Id = "moderator", 
                    Name = "Moderator", 
                    Description = "Moderate user access", 
                    Permissions = { "Permissions.Users.View", "Permissions.Users.Create" } 
                },
                _ => null
            };

            if (role == null)
            {
                return Task.FromResult<IActionResult>(BadRequest(new { Message = "Role not found" }));
            }

            return Task.FromResult<IActionResult>(Ok(role));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get role permissions error: {ex.Message}");
            return Task.FromResult<IActionResult>(BadRequest(new { Error = ex.Message }));
        }
    }

    [HttpPut("roles/{id}/permissions")]
    [Authorize]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public Task<IActionResult> UpdateRolePermissionsAsync(string id, [FromBody] UpdatePermissionsCommand request)
    {
        try
        {
            // For now, just return success
            // In a real implementation, you'd update permissions in database
            if (string.IsNullOrEmpty(id))
            {
                return Task.FromResult<IActionResult>(BadRequest(new { Message = "Role ID is required" }));
            }

            return Task.FromResult<IActionResult>(Ok(new { Message = "Role permissions updated successfully" }));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Update role permissions error: {ex.Message}");
            return Task.FromResult<IActionResult>(BadRequest(new { Error = ex.Message }));
        }
    }
}

public class LoginRequest
{
    public string Email { get; set; } = default!;

    public string Password { get; set; } = default!;
}

public class RegisterRequest
{
    public string Email { get; set; } = default!;

    public string PhoneNumber { get; set; } = default!;

    public string Tckn { get; set; } = default!;

    public string Password { get; set; } = default!;

    public string FirstName { get; set; } = default!;

    public string LastName { get; set; } = default!;

    public DateTime? BirthDate { get; set; }
}

public class TokenGenerationCommand
{
    public string Email { get; set; } = default!;

    public string Password { get; set; } = default!;
}

public class RefreshTokenCommand
{
    public string Token { get; set; } = default!;

    public string RefreshToken { get; set; } = default!;
}

public class TokenResponse
{
    public string Token { get; set; } = default!;

    public string RefreshToken { get; set; } = default!;

    public DateTime RefreshTokenExpiryTime { get; set; }
}

public class ForgotPasswordCommand
{
    public string Email { get; set; } = default!;
}

public class ChangePasswordCommand
{
    public string CurrentPassword { get; set; } = default!;

    public string NewPassword { get; set; } = default!;
}

public class UpdateUserCommand
{
    public string FirstName { get; set; } = default!;

    public string LastName { get; set; } = default!;

    public string PhoneNumber { get; set; } = default!;
}

public class UserDetail
{
    public Guid Id { get; set; }

    public string Email { get; set; } = default!;

    public string FirstName { get; set; } = default!;

    public string LastName { get; set; } = default!;

    public string PhoneNumber { get; set; } = default!;

    public bool IsEmailVerified { get; set; }

    public bool IsPhoneVerified { get; set; }

    public bool IsIdentityVerified { get; set; }

    public string UserName { get; set; } = default!;

    public bool EmailConfirmed { get; set; }

    public bool IsActive { get; set; }
}

public class RegisterUserCommand
{
    public string Email { get; set; } = default!;

    public string PhoneNumber { get; set; } = default!;

    public string Password { get; set; } = default!;

    public string FirstName { get; set; } = default!;

    public string LastName { get; set; } = default!;
}

public class RoleDto
{
    public string Id { get; set; } = default!;

    public string Name { get; set; } = default!;

    public string Description { get; set; } = default!;

    public ICollection<string> Permissions { get; init; } = new List<string>();
}

public class CreateOrUpdateRoleCommand
{
    public string Id { get; set; } = default!;

    public string Name { get; set; } = default!;

    public string Description { get; set; } = default!;
}

public class UserRoleDetail
{
    public string RoleId { get; set; } = default!;

    public string RoleName { get; set; } = default!;

    public string Description { get; set; } = default!;

    public bool Enabled { get; set; }
}

public class AssignUserRoleCommand
{
    public ICollection<UserRoleDetail> UserRoles { get; init; } = new List<UserRoleDetail>();
}

public class AuditTrail
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Operation { get; set; } = default!;

    public string Entity { get; set; } = default!;

    public DateTimeOffset DateTime { get; set; }

    public string? PreviousValues { get; set; }

    public string? NewValues { get; set; }

    public string? ModifiedProperties { get; set; }

    public string PrimaryKey { get; set; } = default!;
}

public class UpdatePermissionsCommand
{
    public ICollection<string> Permissions { get; init; } = new List<string>();
} 
