using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace FSH.Framework.Infrastructure.Auth;

public class DapperUserRepository
{
    private readonly IDbConnection _db;

    public DapperUserRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _db.QueryFirstOrDefaultAsync<User>(
            @"SELECT 
                id,
                email,
                username,
                phone_number,
                tckn,
                password_hash,
                first_name,
                last_name,
                profession,
                birth_date,
                is_identity_verified,
                is_phone_verified,
                is_email_verified,
                status,
                created_at,
                updated_at
            FROM users WHERE email = @Email", new { Email = email });
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _db.QueryFirstOrDefaultAsync<User>(
            @"SELECT 
                id,
                email,
                username,
                phone_number,
                tckn,
                password_hash,
                first_name,
                last_name,
                profession,
                birth_date,
                is_identity_verified,
                is_phone_verified,
                is_email_verified,
                status,
                created_at,
                updated_at
            FROM users WHERE username = @Username", new { Username = username });
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _db.QueryFirstOrDefaultAsync<User>(
            @"SELECT 
                id,
                email,
                username,
                phone_number,
                tckn,
                password_hash,
                first_name,
                last_name,
                profession,
                birth_date,
                is_identity_verified,
                is_phone_verified,
                is_email_verified,
                status,
                created_at,
                updated_at
            FROM users WHERE id = @Id", new { Id = id });
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        var users = await _db.QueryAsync<User>(
            @"SELECT 
                id,
                email,
                username,
                phone_number,
                tckn,
                password_hash,
                first_name,
                last_name,
                profession,
                birth_date,
                is_identity_verified,
                is_phone_verified,
                is_email_verified,
                status,
                created_at,
                updated_at
            FROM users ORDER BY created_at DESC");
        return users.ToList();
    }

    public async Task<bool> ValidatePasswordAsync(string email, string password)
    {
        var user = await GetByEmailAsync(email);
        if (user == null) return false;
        return BCrypt.Net.BCrypt.Verify(password, user.password_hash);
    }

    public async Task<bool> ValidatePasswordAsync(Guid userId, string password)
    {
        var user = await GetByIdAsync(userId);
        if (user == null) return false;
        return BCrypt.Net.BCrypt.Verify(password, user.password_hash);
    }

    public async Task<(bool IsValid, User? User)> ValidatePasswordAndGetUserAsync(string email, string password)
    {
        var user = await GetByEmailAsync(email);
        if (user == null) return (false, null);
        
        var isValid = BCrypt.Net.BCrypt.Verify(password, user.password_hash);
        return (isValid, isValid ? user : null);
    }

    public async Task CreateAsync(User user)
    {
        user.password_hash = BCrypt.Net.BCrypt.HashPassword(user.password_hash);
        await _db.ExecuteAsync(
            @"INSERT INTO users (id, email, username, phone_number, tckn, password_hash, first_name, last_name, profession, birth_date, is_identity_verified, is_phone_verified, is_email_verified, status, created_at, updated_at)
              VALUES (@id, @email, @username, @phone_number, @tckn, @password_hash, @first_name, @last_name, @profession, @birth_date, @is_identity_verified, @is_phone_verified, @is_email_verified, @status, @created_at, @updated_at)",
            user);
    }

    public async Task UpdatePasswordAsync(Guid userId, string newPassword)
    {
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.ExecuteAsync(
            @"UPDATE users SET password_hash = @PasswordHash, updated_at = @UpdatedAt WHERE id = @Id",
            new { PasswordHash = hashedPassword, UpdatedAt = DateTime.UtcNow, Id = userId });
    }

    // Reset password with token
    public async Task ResetPasswordAsync(string email, string newPassword)
    {
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.ExecuteAsync(
            @"UPDATE users SET password_hash = @PasswordHash, updated_at = @UpdatedAt WHERE email = @Email",
            new { PasswordHash = hashedPassword, UpdatedAt = DateTime.UtcNow, Email = email });
    }

    public async Task UpdateAsync(User user)
    {
        await _db.ExecuteAsync(
            @"UPDATE users SET 
                username = @username,
                phone_number = @phone_number,
                profession = @profession,
                updated_at = @updated_at
              WHERE id = @id",
            user);
    }

    // Update email separately for security
    public async Task UpdateEmailAsync(Guid userId, string newEmail)
    {
        await _db.ExecuteAsync(
            @"UPDATE users SET email = @Email, is_email_verified = false, updated_at = @UpdatedAt WHERE id = @Id",
            new { Email = newEmail, UpdatedAt = DateTime.UtcNow, Id = userId });
    }

    // Update phone separately for security
    public async Task UpdatePhoneAsync(Guid userId, string newPhoneNumber)
    {
        await _db.ExecuteAsync(
            @"UPDATE users SET phone_number = @PhoneNumber, is_phone_verified = false, updated_at = @UpdatedAt WHERE id = @Id",
            new { PhoneNumber = newPhoneNumber, UpdatedAt = DateTime.UtcNow, Id = userId });
    }

    // Admin Update User (more comprehensive)
    public async Task AdminUpdateUserAsync(User user)
    {
        await _db.ExecuteAsync(
            @"UPDATE users SET 
                email = @email,
                username = @username,
                phone_number = @phone_number,
                first_name = @first_name,
                last_name = @last_name,
                profession = @profession,
                status = @status,
                is_identity_verified = @is_identity_verified,
                is_phone_verified = @is_phone_verified,
                is_email_verified = @is_email_verified,
                updated_at = @updated_at
              WHERE id = @id",
            user);
    }

    // Email verification
    public async Task MarkEmailAsVerifiedAsync(Guid userId)
    {
        await _db.ExecuteAsync(
            @"UPDATE users SET is_email_verified = true, updated_at = @UpdatedAt WHERE id = @Id",
            new { UpdatedAt = DateTime.UtcNow, Id = userId });
    }

    // Phone verification
    public async Task MarkPhoneAsVerifiedAsync(Guid userId)
    {
        await _db.ExecuteAsync(
            @"UPDATE users SET is_phone_verified = true, updated_at = @UpdatedAt WHERE id = @Id",
            new { UpdatedAt = DateTime.UtcNow, Id = userId });
    }

    // Delete User (Soft delete by status)
    public async Task DeleteUserAsync(Guid userId)
    {
        await _db.ExecuteAsync(
            @"UPDATE users SET status = 'DELETED', updated_at = @UpdatedAt WHERE id = @Id",
            new { UpdatedAt = DateTime.UtcNow, Id = userId });
    }

    // Hard Delete User (permanent)
    public async Task HardDeleteUserAsync(Guid userId)
    {
        await _db.ExecuteAsync("DELETE FROM users WHERE id = @Id", new { Id = userId });
    }

    // Get User Roles
    public async Task<List<string>> GetUserRolesAsync(Guid userId)
    {
        var roles = await _db.QueryAsync<string>(
            @"SELECT r.id 
              FROM roles r 
              INNER JOIN user_roles ur ON r.id = ur.role_id 
              WHERE ur.user_id = @UserId",
            new { UserId = userId });
        return roles.ToList();
    }

    // Assign Role to User
    public async Task AssignRoleToUserAsync(Guid userId, string roleId, Guid? assignedBy = null)
    {
        await _db.ExecuteAsync(
            @"INSERT INTO user_roles (user_id, role_id, assigned_by) 
              VALUES (@UserId, @RoleId, @AssignedBy)
              ON CONFLICT (user_id, role_id) DO NOTHING",
            new { UserId = userId, RoleId = roleId, AssignedBy = assignedBy });
    }

    // Remove Role from User
    public async Task RemoveRoleFromUserAsync(Guid userId, string roleId)
    {
        await _db.ExecuteAsync(
            @"DELETE FROM user_roles WHERE user_id = @UserId AND role_id = @RoleId",
            new { UserId = userId, RoleId = roleId });
    }

    // Check if user has role
    public async Task<bool> UserHasRoleAsync(Guid userId, string roleId)
    {
        var count = await _db.QuerySingleAsync<int>(
            @"SELECT COUNT(*) FROM user_roles WHERE user_id = @UserId AND role_id = @RoleId",
            new { UserId = userId, RoleId = roleId });
        return count > 0;
    }

    // Check if username exists
    public async Task<bool> UsernameExistsAsync(string username, Guid? excludeUserId = null)
    {
        var query = excludeUserId.HasValue
            ? "SELECT COUNT(*) FROM users WHERE username = @Username AND id != @ExcludeUserId"
            : "SELECT COUNT(*) FROM users WHERE username = @Username";
            
        var count = await _db.QuerySingleAsync<int>(query, new { Username = username, ExcludeUserId = excludeUserId });
        return count > 0;
    }

    // Check if email exists
    public async Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null)
    {
        var query = excludeUserId.HasValue
            ? "SELECT COUNT(*) FROM users WHERE email = @Email AND id != @ExcludeUserId"
            : "SELECT COUNT(*) FROM users WHERE email = @Email";
            
        var count = await _db.QuerySingleAsync<int>(query, new { Email = email, ExcludeUserId = excludeUserId });
        return count > 0;
    }

    // Get Users by Role
    public async Task<List<User>> GetUsersByRoleAsync(string roleId)
    {
        var users = await _db.QueryAsync<User>(
            @"SELECT u.id, u.email, u.username, u.phone_number, u.tckn, u.password_hash, 
                     u.first_name, u.last_name, u.profession, u.birth_date, u.is_identity_verified, 
                     u.is_phone_verified, u.is_email_verified, u.status, u.created_at, u.updated_at
              FROM users u 
              INNER JOIN user_roles ur ON u.id = ur.user_id 
              WHERE ur.role_id = @RoleId AND u.status != 'DELETED'
              ORDER BY u.created_at DESC",
            new { RoleId = roleId });
        return users.ToList();
    }
} 