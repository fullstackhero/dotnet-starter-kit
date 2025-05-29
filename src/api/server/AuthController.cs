using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using FSH.Framework.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Asp.Versioning;

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