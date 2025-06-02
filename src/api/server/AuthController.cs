using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Asp.Versioning;
using BCrypt.Net;
using FSH.Framework.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FSH.Starter.WebApi.Host;

[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[ApiVersion("1.0")]
public class AuthController : ControllerBase
{
    private readonly DapperUserRepository _userRepository;
    private readonly IMernisService _mernisService;
    private readonly IPasswordResetService _passwordResetService;
    private readonly string _jwtSecret;

    public AuthController(IDbConnection db, IConfiguration config, IMernisService mernisService, IPasswordResetService passwordResetService)
    {
        _userRepository = new DapperUserRepository(db);
        _mernisService = mernisService;
        _passwordResetService = passwordResetService;
        _jwtSecret = config.GetValue<string>("JwtOptions:Key") ?? "your-super-secret-jwt-key-here-min-32-chars";

        Console.WriteLine($"AuthController initialized with JWT secret length: {_jwtSecret.Length}");
    }

    // PUBLIC ENDPOINTS START HERE
    [HttpGet("test")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult Test()
    {
        return Ok(new { Message = "Auth API is working!" });
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
            // Validate TCKN format first
            if (!IsValidTCKN(request.Tckn))
            {
                return BadRequest(new { Message = "Invalid TCKN format" });
            }

            // Validate password and get user by TCKN in single query (optimized)
            var (isValid, user) = await _userRepository.ValidatePasswordAndGetUserByTcknAsync(request.Tckn, request.Password);
            
            if (!isValid || user == null)
            {
                return Unauthorized(new { Message = "Invalid TCKN or password" });
            }

            if (user.status != "ACTIVE")
            {
                return Unauthorized(new { Message = "Account is not active" });
            }

            var userRoles = await _userRepository.GetUserRolesAsync(user.id);
            var token = JwtHelper.GenerateToken(user, _jwtSecret, userRoles);

            return Ok(new 
            { 
                Token = token, 
                User = new 
                { 
                    user.id, 
                    user.email, 
                    user.username,
                    user.first_name, 
                    user.last_name,
                    user.profession,
                    Roles = userRoles 
                } 
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
            return BadRequest(new { Error = "Login failed" });
        }
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest request)
    {
        try
        {
            Console.WriteLine($"[DEBUG] Register method called for email: {request.Email}");

            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.Tckn))
            {
                return BadRequest(new { Message = "All required fields must be provided" });
            }

            // Validate email format
            if (!IsValidEmail(request.Email))
            {
                return BadRequest(new { Message = "Invalid email format" });
            }

            // Validate password strength
            var (isValidPassword, passwordError) = ValidatePassword(request.Password);
            if (!isValidPassword)
            {
                return BadRequest(new { Message = passwordError });
            }

            // Validate phone number format
            if (!IsValidPhoneNumber(request.PhoneNumber))
            {
                return BadRequest(new { Message = "Invalid phone number format. Use format: +90XXXXXXXXX" });
            }

            // Validate TCKN
            if (!IsValidTCKN(request.Tckn))
            {
                return BadRequest(new { Message = "Invalid TCKN format" });
            }

            // Validate username format
            if (!IsValidUsername(request.Username))
            {
                return BadRequest(new { Message = "Username must be 3-20 characters long and contain only letters, numbers, and underscores" });
            }

            // Check if email already exists
            if (await _userRepository.EmailExistsAsync(request.Email))
            {
                return BadRequest(new { Message = "Email already exists" });
            }

            // Check if username already exists
            if (await _userRepository.UsernameExistsAsync(request.Username))
            {
                return BadRequest(new { Message = "Username already exists" });
            }

            // MERNİS Identity Verification
            Console.WriteLine($"[DEBUG] Starting MERNİS verification for TCKN: {request.Tckn}");
            var birthYear = request.BirthDate?.Year ?? DateTime.UtcNow.AddYears(-25).Year;
            Console.WriteLine($"[DEBUG] Birth year: {birthYear}");
            Console.WriteLine($"[DEBUG] Name: {request.FirstName} {request.LastName}");
            
            var isMernisVerified = await _mernisService.VerifyIdentityAsync(
                request.Tckn, 
                request.FirstName, 
                request.LastName, 
                birthYear);

            Console.WriteLine($"[DEBUG] MERNİS verification result: {isMernisVerified}");

            if (!isMernisVerified)
            {
                Console.WriteLine($"[DEBUG] MERNİS verification failed, returning error");
                return BadRequest(new { Message = "Identity verification failed. Please check your TCKN, name, surname and birth year." });
            }

            Console.WriteLine($"[DEBUG] MERNİS verification passed, proceeding with user creation");

            var user = new User
            {
                id = Guid.NewGuid(),
                email = request.Email,
                username = request.Username,
                phone_number = request.PhoneNumber,
                tckn = request.Tckn,
                password_hash = request.Password, // Bu BCrypt ile hash'lenecek
                first_name = request.FirstName,
                last_name = request.LastName,
                profession = request.Profession ?? string.Empty,
                birth_date = request.BirthDate ?? DateTime.UtcNow.AddYears(-25), // Default if null
                is_identity_verified = false,
                is_phone_verified = false,
                is_email_verified = false,
                status = "ACTIVE",
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            };

            await _userRepository.CreateAsync(user);
            
            // Assign base_user role by default
            await _userRepository.AssignRoleToUserAsync(user.id, RoleConstants.BaseUser);
            
            return Ok(new { Message = "User created successfully", UserId = user.id });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Register error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
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
            Console.WriteLine($"Token generation attempt for TCKN: {request.Tckn}");
            
            // Validate TCKN format first
            if (!IsValidTCKN(request.Tckn))
            {
                return BadRequest(new { Message = "Invalid TCKN format" });
            }
            
            // Validate password and get user by TCKN in single query (optimized)
            var (isValid, user) = await _userRepository.ValidatePasswordAndGetUserByTcknAsync(request.Tckn, request.Password);
            
            if (!isValid || user == null)
            {
                Console.WriteLine("Authentication failed - user not found or invalid password");
                return Unauthorized();
            }

            // Get user roles
            var userRoles = await _userRepository.GetUserRolesAsync(user.id);
            
            var token = JwtHelper.GenerateToken(user, _jwtSecret, userRoles);
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
    public IActionResult GetPermissions()
    {
        try
        {
            var roles = GetCurrentUserRoles();
            
            // Return permissions based on roles
            var permissions = new List<string>();
            
            if (roles.Contains(RoleConstants.Admin))
            {
                permissions.AddRange(new[] { "Permissions.All" });
            }
            else if (roles.Contains(RoleConstants.CustomerAdmin))
            {
                permissions.AddRange(new[] { "Permissions.Users.Create", "Permissions.Users.Update", "Permissions.Users.View", "Permissions.Users.Delete" });
            }
            else if (roles.Contains(RoleConstants.CustomerSupport))
            {
                permissions.AddRange(new[] { "Permissions.Users.View", "Permissions.Users.Update" });
            }
            else
            {
                permissions.AddRange(new[] { "Permissions.Profile.View", "Permissions.Profile.Update" });
            }
                
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
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordCommand request)
    {
        try
        {
            // Validate email format
            if (!IsValidEmail(request.Email))
            {
                return BadRequest(new { Message = "Invalid email format" });
            }

            // Check if user exists (but don't reveal if user doesn't exist for security)
            var user = await _userRepository.GetByEmailAsync(request.Email);
            
            // Always return success to prevent email enumeration attacks
            if (user == null)
            {
                // Still return success but don't actually send email
                return Ok(new { Message = "If the email exists in our system, a password reset link has been sent." });
            }

            try
            {
                // Generate secure reset token
                var resetToken = await _passwordResetService.GenerateResetTokenAsync(request.Email);
                
                // Email integration placeholder - in production this would send actual email
                // For development: token is logged to console
                // For production: implement proper email service integration
                var resetLink = $"{Request.Scheme}://{Request.Host}/reset-password?email={Uri.EscapeDataString(request.Email)}&token={Uri.EscapeDataString(resetToken)}";
                
                Console.WriteLine($"Password reset token for {request.Email}: {resetToken}");
                Console.WriteLine($"Reset link: {resetLink}");
                Console.WriteLine("In production, this would be sent via email instead of logged.");
                
                return Ok(new { Message = "If the email exists in our system, a password reset link has been sent." });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Too many", StringComparison.Ordinal))
            {
                return BadRequest(new { Message = "Too many password reset attempts. Please try again later." });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Forgot password error: {ex.Message}");
            return BadRequest(new { Error = "An error occurred while processing your request." });
        }
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordCommand request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.NewPassword))
            {
                return BadRequest(new { Message = "Email, token, and new password are required." });
            }

            // Validate email format
            if (!IsValidEmail(request.Email))
            {
                return BadRequest(new { Message = "Invalid email format" });
            }

            // Validate new password strength
            var passwordValidationResult = ValidatePassword(request.NewPassword);
            if (!passwordValidationResult.IsValid)
            {
                return BadRequest(new { Message = passwordValidationResult.ErrorMessage });
            }

            // Validate reset token
            var isValidToken = await _passwordResetService.ValidateResetTokenAsync(request.Email, request.Token);
            if (!isValidToken)
            {
                return BadRequest(new { Message = "Invalid or expired reset token." });
            }

            // Get user
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return BadRequest(new { Message = "User not found." });
            }

            // Update password
            await _userRepository.UpdatePasswordAsync(user.id, request.NewPassword);
            
            // Invalidate any remaining reset tokens for this user
            await _passwordResetService.InvalidateResetTokenAsync(request.Email);

            return Ok(new { Message = "Password has been reset successfully. You can now login with your new password." });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Reset password error: {ex.Message}");
            return BadRequest(new { Error = "An error occurred while resetting your password." });
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
            // Validate new password strength
            var passwordValidationResult = ValidatePassword(request.NewPassword);
            if (!passwordValidationResult.IsValid)
            {
                return BadRequest(new { Message = passwordValidationResult.ErrorMessage });
            }

            // Get current user ID from token (consistent with other endpoints)
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BadRequest(new { Message = "Unable to determine current user" });
            }

            // Validate current password using user ID (no need to fetch user twice)
            if (!await _userRepository.ValidatePasswordAsync(currentUserId, request.CurrentPassword))
            {
                return BadRequest(new { Message = "Current password is incorrect" });
            }

            await _userRepository.UpdatePasswordAsync(currentUserId, request.NewPassword);
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
            var currentUserId = GetCurrentUserId();
            
            if (currentUserId == Guid.Empty)
            {
                return BadRequest(new { Message = "Unable to determine current user" });
            }

            var user = await _userRepository.GetByIdAsync(currentUserId);
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
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BadRequest(new { Message = "Unable to determine current user" });
            }

            var user = await _userRepository.GetByIdAsync(currentUserId);
            if (user == null)
            {
                return BadRequest(new { Message = "User not found" });
            }

            // Check if username is being changed and if it's available
            if (!string.IsNullOrEmpty(request.Username) && request.Username != user.username && await _userRepository.UsernameExistsAsync(request.Username, user.id))
            {
                return BadRequest(new { Message = "Username already exists" });
            }

            // Only update allowed fields
            user.username = request.Username ?? user.username;
            user.profession = request.Profession ?? user.profession;
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

    [HttpPut("profile/email")]
    [Authorize]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdateEmailAsync([FromBody] UpdateEmailCommand request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BadRequest(new { Message = "Unable to determine current user" });
            }

            // Check if email already exists
            if (await _userRepository.EmailExistsAsync(request.NewEmail, currentUserId))
            {
                return BadRequest(new { Message = "Email already exists" });
            }

            await _userRepository.UpdateEmailAsync(currentUserId, request.NewEmail);
            return Ok(new { Message = "Email updated successfully. Please verify your new email." });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Update email error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPut("profile/phone")]
    [Authorize]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdatePhoneAsync([FromBody] UpdatePhoneCommand request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BadRequest(new { Message = "Unable to determine current user" });
            }

            await _userRepository.UpdatePhoneAsync(currentUserId, request.NewPhoneNumber);
            return Ok(new { Message = "Phone number updated successfully. Please verify your new phone number." });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Update phone error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("verify-email")]
    [Authorize]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> VerifyEmailAsync([FromBody] VerifyEmailCommand request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BadRequest(new { Message = "Unable to determine current user" });
            }

            // IMPLEMENTATION NOTE: This is a simplified demo verification system.
            // In production, implement: database storage, expiration tracking, 
            // email service integration, rate limiting, and secure code generation.
            // 1. Store verification codes in database/cache with expiration
            // 2. Send verification email with the code
            // 3. Validate the code against stored value
            // 4. Check code expiration time
            // 5. Implement rate limiting for verification attempts

            // For now, we'll use a simple validation (for demo purposes only)
            if (string.IsNullOrEmpty(request.VerificationCode) || request.VerificationCode.Length != 6)
            {
                return BadRequest(new { Message = "Invalid verification code format. Code must be 6 digits." });
            }

            if (!request.VerificationCode.All(char.IsDigit))
            {
                return BadRequest(new { Message = "Verification code must contain only digits." });
            }

            // SECURITY WARNING: This is a demo implementation
            // In production, implement proper verification code validation
            await _userRepository.MarkEmailAsVerifiedAsync(currentUserId);
            return Ok(new { Message = "Email verified successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Verify email error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("verify-phone")]
    [Authorize]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> VerifyPhoneAsync([FromBody] VerifyPhoneCommand request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BadRequest(new { Message = "Unable to determine current user" });
            }

            // IMPLEMENTATION NOTE: This is a simplified demo verification system.
            // In production, implement: database storage, expiration tracking,
            // SMS service integration, rate limiting, and secure code generation.
            // 1. Store verification codes in database/cache with expiration  
            // 2. Send SMS with the verification code
            // 3. Validate the code against stored value
            // 4. Check code expiration time
            // 5. Implement rate limiting for verification attempts

            // For now, we'll use a simple validation (for demo purposes only)
            if (string.IsNullOrEmpty(request.VerificationCode) || request.VerificationCode.Length != 6)
            {
                return BadRequest(new { Message = "Invalid verification code format. Code must be 6 digits." });
            }

            if (!request.VerificationCode.All(char.IsDigit))
            {
                return BadRequest(new { Message = "Verification code must contain only digits." });
            }

            // SECURITY WARNING: This is a demo implementation
            // In production, implement proper SMS verification code validation
            await _userRepository.MarkPhoneAsVerifiedAsync(currentUserId);
            return Ok(new { Message = "Phone verified successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Verify phone error: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    private static (bool IsValid, string ErrorMessage) ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return (false, "Password is required");
        }

        if (password.Length < 8)
        {
            return (false, "Password must be at least 8 characters long");
        }

        if (!password.Any(char.IsUpper))
        {
            return (false, "Password must contain at least one uppercase letter");
        }

        if (!password.Any(char.IsLower))
        {
            return (false, "Password must contain at least one lowercase letter");
        }

        if (!password.Any(char.IsDigit))
        {
            return (false, "Password must contain at least one number");
        }

        var specialChars = "!@#$%^&*()_+[]{}|;:,.<>?";
        if (!password.Any(c => specialChars.Contains(c)))
        {
            return (false, "Password must contain at least one special character");
        }

        return (true, string.Empty);
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            // Normalize the domain
            email = Regex.Replace(
                email,
                @"(@)(.+)$", 
                DomainMapper,
                RegexOptions.None, 
                TimeSpan.FromMilliseconds(200));

            // Examines the domain part of the email and normalizes it.
            static string DomainMapper(Match match)
            {
                // Use IdnMapping class to convert Unicode domain names.
                var idn = new IdnMapping();

                // Pull out and process domain name (throws ArgumentException on invalid)
                string domainName = idn.GetAscii(match.Groups[2].Value);

                return match.Groups[1].Value + domainName;
            }
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }

        try
        {
            return Regex.IsMatch(
                email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase, 
                TimeSpan.FromMilliseconds(250));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private static bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return false;
        }

        // Turkish phone number pattern: +90 followed by 10 digits
        var pattern = @"^\+90[0-9]{10}$";
        return Regex.IsMatch(phoneNumber, pattern);
    }

    private static bool IsValidTCKN(string tckn)
    {
        if (string.IsNullOrWhiteSpace(tckn) || tckn.Length != 11)
        {
            return false;
        }

        if (!tckn.All(char.IsDigit))
        {
            return false;
        }

        // TCKN cannot start with 0
        if (tckn[0] == '0')
        {
            return false;
        }

        // Algorithm for Turkish National ID validation
        var digits = tckn.Select(c => int.Parse(c.ToString())).ToArray();

        var sumOdd = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        var sumEven = digits[1] + digits[3] + digits[5] + digits[7];

        var check1 = ((sumOdd * 7) - sumEven) % 10;
        var check2 = (sumOdd + sumEven + check1) % 10;

        return check1 == digits[9] && check2 == digits[10];
    }

    private static bool IsValidUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        if (username.Length < 3 || username.Length > 20)
        {
            return false;
        }

        // Username can contain letters, numbers, and underscores
        var pattern = @"^[a-zA-Z0-9_]+$";
        return Regex.IsMatch(username, pattern);
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
}

// REQUEST/RESPONSE MODELS
public class LoginRequest
{
    public string Tckn { get; set; } = default!;
    public string Password { get; set; } = default!;
}

public class RegisterRequest
{
    public string Email { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string Tckn { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? Profession { get; set; }
    public DateTime? BirthDate { get; set; }
}

public class AssignRoleCommand
{
    public string RoleId { get; set; } = default!;
}

public class TokenGenerationCommand
{
    public string Tckn { get; set; } = default!;
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

public class ResetPasswordCommand
{
    public string Email { get; set; } = default!;
    public string Token { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
}

public class ChangePasswordCommand
{
    public string CurrentPassword { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
}

public class UpdateUserCommand
{
    public string? Username { get; set; }
    public string? Profession { get; set; }
}

public class UpdateEmailCommand
{
    public string NewEmail { get; set; } = default!;
}

public class UpdatePhoneCommand
{
    public string NewPhoneNumber { get; set; } = default!;
}

public class VerifyEmailCommand
{
    public string VerificationCode { get; set; } = default!;
}

public class VerifyPhoneCommand
{
    public string VerificationCode { get; set; } = default!;
}

public class UserDetail
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string? Profession { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsPhoneVerified { get; set; }
    public bool IsIdentityVerified { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<string> Roles { get; init; } = new List<string>();
}

public class RoleDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
} 
