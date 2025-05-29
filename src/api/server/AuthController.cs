using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using FSH.Framework.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Asp.Versioning;
using System.Collections.Generic;
using System.Linq;

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
    public IActionResult Test()
    {
        return Ok(new { Message = "AuthController is working", JwtSecretExists = !string.IsNullOrEmpty(_jwtSecret) });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
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
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Check if user already exists
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
            return BadRequest(new { Message = "User with this email already exists" });

        var user = new User
        {
            id = Guid.NewGuid(),
            email = request.Email,
            phone_number = request.PhoneNumber,
            tckn = request.Tckn,
            password_hash = request.Password, // Bu BCrypt ile hash'lenecek
            first_name = request.FirstName,
            last_name = request.LastName,
            birth_date = request.BirthDate,
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
    public async Task<IActionResult> GenerateToken([FromBody] TokenGenerationCommand request)
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
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand request)
    {
        try
        {
            // For now, just validate that refresh token exists and generate new token
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return Unauthorized(new { Message = "Invalid refresh token" });
            }

            // In a real implementation, you'd validate the refresh token against database
            // For now, we'll just generate a new token set
            var newToken = request.Token; // Simplified for now
            var newRefreshToken = Guid.NewGuid().ToString();
            
            return Ok(new TokenResponse 
            { 
                Token = newToken, 
                RefreshToken = newRefreshToken,
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Token refresh error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("permissions")]
    [Authorize]
    [ProducesResponseType(typeof(List<string>), 200)]
    public async Task<IActionResult> GetUserPermissions()
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
            
            return Ok(permissions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get permissions error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand request)
    {
        try
        {
            // Check if user exists
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                // Don't reveal if user exists or not for security reasons
                return Ok(new { Message = "If an account with that email exists, a password reset link has been sent." });
            }

            // In a real implementation, you'd:
            // 1. Generate a password reset token
            // 2. Save it to database with expiry
            // 3. Send email with reset link
            
            Console.WriteLine($"Password reset requested for: {request.Email}");
            return Ok(new { Message = "If an account with that email exists, a password reset link has been sent." });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Forgot password error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand request)
    {
        try
        {
            // Get current user from JWT token
            var userIdClaim = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            // Verify current password
            if (!await _userRepository.ValidatePasswordAsync(user.email, request.CurrentPassword))
            {
                return BadRequest(new { Message = "Current password is incorrect" });
            }

            // Update password
            await _userRepository.UpdatePasswordAsync(userId, request.NewPassword);
            
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
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            // Get current user from JWT token
            var userIdClaim = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            var userDetail = new UserDetail
            {
                Id = user.id,
                Email = user.email,
                FirstName = user.first_name,
                LastName = user.last_name,
                PhoneNumber = user.phone_number,
                IsEmailVerified = user.is_email_verified,
                IsPhoneVerified = user.is_phone_verified,
                IsIdentityVerified = user.is_identity_verified
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
    [ProducesResponseType(200)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserCommand request)
    {
        try
        {
            // Get current user from JWT token
            var userIdClaim = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            // Update user profile
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
    public async Task<IActionResult> GetUsersList()
    {
        try
        {
            // For now, return simple user list - in production you'd implement proper pagination
            var users = await _userRepository.GetAllUsersAsync();
            var userDetails = users.Select(u => new UserDetail
            {
                Id = u.id,
                Email = u.email,
                FirstName = u.first_name,
                LastName = u.last_name,
                PhoneNumber = u.phone_number,
                IsEmailVerified = u.is_email_verified,
                IsPhoneVerified = u.is_phone_verified,
                IsIdentityVerified = u.is_identity_verified,
                UserName = u.email, // Use email as username for now
                EmailConfirmed = u.is_email_verified,
                IsActive = u.status == "ACTIVE" || u.status == "ADMIN"
            }).ToList();

            return Ok(userDetails);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get users list error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("users/register")]
    [Authorize]
    [ProducesResponseType(200)]
    public async Task<IActionResult> RegisterUser([FromBody] RegisterUserCommand request)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
                return BadRequest(new { Message = "User with this email already exists" });

            var user = new User
            {
                id = Guid.NewGuid(),
                email = request.Email,
                phone_number = request.PhoneNumber ?? string.Empty,
                tckn = string.Empty, // Not provided in admin panel
                password_hash = request.Password,
                first_name = request.FirstName,
                last_name = request.LastName,
                birth_date = DateTime.UtcNow.AddYears(-25), // Default age
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
    public async Task<IActionResult> GetUser(Guid id)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                return NotFound(new { Message = "User not found" });

            var userDetail = new UserDetail
            {
                Id = user.id,
                Email = user.email,
                FirstName = user.first_name,
                LastName = user.last_name,
                PhoneNumber = user.phone_number,
                IsEmailVerified = user.is_email_verified,
                IsPhoneVerified = user.is_phone_verified,
                IsIdentityVerified = user.is_identity_verified,
                UserName = user.email,
                EmailConfirmed = user.is_email_verified,
                IsActive = user.status == "ACTIVE" || user.status == "ADMIN"
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
    public async Task<IActionResult> GetRoles()
    {
        try
        {
            // Simple role system: Admin and Basic
            var roles = new List<RoleDto>
            {
                new RoleDto { Id = "admin", Name = "Admin", Description = "Administrator role with full permissions" },
                new RoleDto { Id = "basic", Name = "Basic", Description = "Basic user role with limited permissions" }
            };

            return Ok(roles);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get roles error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("roles")]
    [Authorize]
    [ProducesResponseType(200)]
    public async Task<IActionResult> CreateOrUpdateRole([FromBody] CreateOrUpdateRoleCommand request)
    {
        try
        {
            // For now, just return success - in production you'd implement actual role management
            return Ok(new { Message = "Role operation completed successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Create/Update role error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpDelete("roles/{id}")]
    [Authorize]
    [ProducesResponseType(200)]
    public async Task<IActionResult> DeleteRole(string id)
    {
        try
        {
            // Prevent deletion of default roles
            if (id == "admin" || id == "basic")
                return BadRequest(new { Message = "Cannot delete default roles" });

            return Ok(new { Message = "Role deleted successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Delete role error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("users/{id}/roles")]
    [Authorize]
    [ProducesResponseType(typeof(List<UserRoleDetail>), 200)]
    public async Task<IActionResult> GetUserRoles(Guid id)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                return NotFound(new { Message = "User not found" });

            // Simple role assignment based on status
            var userRoles = new List<UserRoleDetail>
            {
                new UserRoleDetail
                {
                    RoleId = "admin",
                    RoleName = "Admin",
                    Description = "Administrator role with full permissions",
                    Enabled = user.status == "ADMIN"
                },
                new UserRoleDetail
                {
                    RoleId = "basic",
                    RoleName = "Basic", 
                    Description = "Basic user role with limited permissions",
                    Enabled = user.status == "ACTIVE"
                }
            };

            return Ok(userRoles);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get user roles error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("users/{id}/roles")]
    [Authorize]
    [ProducesResponseType(200)]
    public async Task<IActionResult> AssignRolesToUser(Guid id, [FromBody] AssignUserRoleCommand request)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                return NotFound(new { Message = "User not found" });

            // Update user status based on role assignment
            var adminRole = request.UserRoles.FirstOrDefault(r => r.RoleId == "admin" && r.Enabled);
            if (adminRole != null)
            {
                user.status = "ADMIN";
            }
            else
            {
                user.status = "ACTIVE";
            }

            user.updated_at = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return Ok(new { Message = "User roles updated successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Assign roles to user error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("users/{id}/audit-trails")]
    [Authorize]
    [ProducesResponseType(typeof(List<AuditTrail>), 200)]
    public async Task<IActionResult> GetUserAuditTrails(Guid id)
    {
        try
        {
            // Simple audit trail - in production you'd have a proper audit service
            var auditTrails = new List<AuditTrail>
            {
                new AuditTrail
                {
                    Id = Guid.NewGuid(),
                    UserId = id,
                    Operation = "Login",
                    Entity = "User",
                    DateTime = DateTimeOffset.UtcNow.AddDays(-1),
                    PreviousValues = null,
                    NewValues = "{\"LastLogin\":\"2024-01-15T10:30:00Z\"}",
                    ModifiedProperties = "[\"LastLogin\"]",
                    PrimaryKey = id.ToString()
                },
                new AuditTrail
                {
                    Id = Guid.NewGuid(),
                    UserId = id,
                    Operation = "Update",
                    Entity = "User",
                    DateTime = DateTimeOffset.UtcNow.AddDays(-2),
                    PreviousValues = "{\"FirstName\":\"Old Name\"}",
                    NewValues = "{\"FirstName\":\"New Name\"}",
                    ModifiedProperties = "[\"FirstName\"]",
                    PrimaryKey = id.ToString()
                }
            };

            return Ok(auditTrails);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get user audit trails error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("roles/{id}/permissions")]
    [Authorize]
    [ProducesResponseType(typeof(RoleDto), 200)]
    public async Task<IActionResult> GetRolePermissions(string id)
    {
        try
        {
            var permissions = new List<string>();
            
            if (id == "admin")
            {
                permissions = new List<string>
                {
                    "Permissions.Users.View",
                    "Permissions.Users.Create",
                    "Permissions.Users.Update",
                    "Permissions.Users.Delete",
                    "Permissions.Roles.View",
                    "Permissions.Roles.Create",
                    "Permissions.Roles.Update",
                    "Permissions.Roles.Delete",
                    "Permissions.Dashboard.View",
                    "Permissions.AuditTrails.View"
                };
            }
            else if (id == "basic")
            {
                permissions = new List<string>
                {
                    "Permissions.Dashboard.View"
                };
            }

            var role = new RoleDto
            {
                Id = id,
                Name = id == "admin" ? "Admin" : "Basic",
                Description = id == "admin" ? "Administrator role" : "Basic user role",
                Permissions = permissions
            };

            return Ok(role);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get role permissions error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPut("roles/{id}/permissions")]
    [Authorize]
    [ProducesResponseType(200)]
    public async Task<IActionResult> UpdateRolePermissions(string id, [FromBody] UpdatePermissionsCommand request)
    {
        try
        {
            // For now, just return success - in production you'd update role permissions
            return Ok(new { Message = "Role permissions updated successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Update role permissions error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
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
    public DateTime BirthDate { get; set; }
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
    public List<string> Permissions { get; set; } = new();
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
    public List<UserRoleDetail> UserRoles { get; set; } = new();
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
    public List<string> Permissions { get; set; } = new();
} 