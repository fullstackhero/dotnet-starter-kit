using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using FSH.Framework.Core.Auth.Domain;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Common.Models;
using FSH.Framework.Infrastructure.Persistence;
using FSH.Framework.Core.Auth.Domain.ValueObjects;
using FSH.Framework.Core.Common.Exceptions;
using EmailVO = FSH.Framework.Core.Auth.Domain.ValueObjects.Email;
using PhoneNumberVO = FSH.Framework.Core.Auth.Domain.ValueObjects.PhoneNumber;
using TcknVO = FSH.Framework.Core.Auth.Domain.ValueObjects.Tckn;
using BCrypt.Net;

namespace FSH.Framework.Infrastructure.Auth;

public sealed class DapperUserRepository : IUserRepository
{
    private readonly IDbConnection _connection;
    private readonly ILogger<DapperUserRepository> _logger;

    public DapperUserRepository(IDbConnection connection, ILogger<DapperUserRepository> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AppUser?> GetByIdAsync(Guid id)
    {
        try
        {
            const string sql = @"
                SELECT 
                    id, email, username, tckn, first_name, last_name, 
                    phone_number, profession, birth_date, password_hash,
                    is_identity_verified, is_phone_verified, is_email_verified,
                    status, created_at, updated_at
                FROM users 
                WHERE id = @Id";

            var user = await _connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
            return user == null ? null : MapToAppUser(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by id {UserId}", id);
            throw new FshException($"Error getting user by id {id}", ex);
        }
    }

    public async Task<AppUser?> GetByEmailAsync(string email)
    {
        ArgumentNullException.ThrowIfNull(email);

        try
        {
            const string sql = @"
                SELECT 
                    id, email, username, tckn, first_name, last_name, 
                    phone_number, profession, birth_date, password_hash,
                    is_identity_verified, is_phone_verified, is_email_verified,
                    status, created_at, updated_at
                FROM users 
                WHERE email = @Email";

            var normalizedEmail = email.ToLowerInvariant();
            var user = await _connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Email = normalizedEmail });
            return user == null ? null : MapToAppUser(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email {Email}", email);
            throw new FshException($"Error getting user by email {email}", ex);
        }
    }

    public async Task<AppUser?> GetByTcknAsync(TcknVO tckn)
    {
        ArgumentNullException.ThrowIfNull(tckn);

        try
        {
            const string sql = @"
                SELECT 
                    id, email, username, tckn, first_name, last_name, 
                    phone_number, profession, birth_date, password_hash,
                    is_identity_verified, is_phone_verified, is_email_verified,
                    status, created_at, updated_at
                FROM users 
                WHERE tckn = @Tckn";

            var user = await _connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Tckn = tckn.Value });
            return user == null ? null : MapToAppUser(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by TCKN {Tckn}", tckn.Value);
            throw new FshException($"Error getting user by TCKN {tckn.Value}", ex);
        }
    }

    public async Task<AppUser?> GetByUsernameAsync(string username)
    {
        ArgumentNullException.ThrowIfNull(username);

        try
        {
            const string sql = @"
                SELECT 
                    id, email, username, tckn, first_name, last_name, 
                    phone_number, profession, birth_date, password_hash,
                    is_identity_verified, is_phone_verified, is_email_verified,
                    status, created_at, updated_at
                FROM users 
                WHERE username = @Username";

            var normalizedUsername = username.ToLowerInvariant();
            var user = await _connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Username = normalizedUsername });
            return user == null ? null : MapToAppUser(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by username {Username}", username);
            throw new FshException($"Error getting user by username {username}", ex);
        }
    }

    public async Task<bool> EmailExistsAsync(string email, Guid? excludeId = null)
    {
        ArgumentNullException.ThrowIfNull(email);

        try
        {
            var normalizedEmail = email.ToLowerInvariant();
            var sql = "SELECT COUNT(1) FROM users WHERE email = @Email";
            var parameters = new DynamicParameters();
            parameters.Add("@Email", normalizedEmail);
            
            if (excludeId.HasValue)
            {
                sql += " AND id != @ExcludeId";
                parameters.Add("@ExcludeId", excludeId.Value);
            }

            var count = await _connection.ExecuteScalarAsync<int>(sql, parameters);
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if email exists {Email}", email);
            throw new FshException($"Error checking if email exists {email}", ex);
        }
    }

    public async Task<bool> UsernameExistsAsync(string username, Guid? excludeId = null)
    {
        ArgumentNullException.ThrowIfNull(username);

        try
        {
            var normalizedUsername = username.ToLowerInvariant();
            var sql = "SELECT COUNT(1) FROM users WHERE username = @Username";
            var parameters = new DynamicParameters();
            parameters.Add("@Username", normalizedUsername);
            
            if (excludeId.HasValue)
            {
                sql += " AND id != @ExcludeId";
                parameters.Add("@ExcludeId", excludeId.Value);
            }

            var count = await _connection.ExecuteScalarAsync<int>(sql, parameters);
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if username exists {Username}", username);
            throw new FshException($"Error checking if username exists {username}", ex);
        }
    }

    public async Task<bool> PhoneExistsAsync(string phoneNumber, Guid? excludeId = null)
    {
        ArgumentNullException.ThrowIfNull(phoneNumber);

        try
        {
            var normalizedPhone = phoneNumber.Trim();
            var sql = "SELECT COUNT(1) FROM users WHERE phone_number = @PhoneNumber";
            var parameters = new DynamicParameters();
            parameters.Add("@PhoneNumber", normalizedPhone);
            
            if (excludeId.HasValue)
            {
                sql += " AND id != @ExcludeId";
                parameters.Add("@ExcludeId", excludeId.Value);
            }

            var count = await _connection.ExecuteScalarAsync<int>(sql, parameters);
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if phone exists {Phone}", phoneNumber);
            throw new FshException($"Error checking if phone exists {phoneNumber}", ex);
        }
    }

    public async Task<Guid> CreateUserAsync(AppUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        try
        {
            var sql = @"
                INSERT INTO users (
                    id, email, username, phone_number, tckn, password_hash,
                    first_name, last_name, profession, birth_date,
                    is_identity_verified, is_phone_verified, is_email_verified,
                    status, created_at, updated_at
                ) VALUES (
                    @Id, @Email, @Username, @PhoneNumber, @Tckn, @PasswordHash,
                    @FirstName, @LastName, @Profession, @BirthDate,
                    @IsIdentityVerified, @IsPhoneVerified, @IsEmailVerified,
                    @Status, @CreatedAt, @UpdatedAt
                )";

            var parameters = new
            {
                user.Id,
                Email = user.Email.Value,
                user.Username,
                PhoneNumber = user.PhoneNumber.Value,
                Tckn = user.Tckn.Value,
                user.PasswordHash,
                user.FirstName,
                user.LastName,
                user.Profession,
                user.BirthDate,
                user.IsIdentityVerified,
                user.IsPhoneVerified,
                user.IsEmailVerified,
                user.Status,
                user.CreatedAt,
                user.UpdatedAt
            };

            await _connection.ExecuteAsync(sql, parameters);
            return user.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {UserId}", user.Id);
            throw new FshException($"Error creating user {user.Id}", ex);
        }
    }

    public async Task UpdateUserAsync(AppUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        try
        {
            var sql = @"
                UPDATE users SET
                    email = @Email,
                    username = @Username,
                    phone_number = @PhoneNumber,
                    tckn = @Tckn,
                    password_hash = @PasswordHash,
                    first_name = @FirstName,
                    last_name = @LastName,
                    profession = @Profession,
                    birth_date = @BirthDate,
                    is_identity_verified = @IsIdentityVerified,
                    is_phone_verified = @IsPhoneVerified,
                    is_email_verified = @IsEmailVerified,
                    status = @Status,
                    updated_at = @UpdatedAt
                WHERE id = @Id";

            var parameters = new
            {
                user.Id,
                Email = user.Email.Value,
                user.Username,
                PhoneNumber = user.PhoneNumber.Value,
                Tckn = user.Tckn.Value,
                user.PasswordHash,
                user.FirstName,
                user.LastName,
                user.Profession,
                user.BirthDate,
                user.IsIdentityVerified,
                user.IsPhoneVerified,
                user.IsEmailVerified,
                user.Status,
                user.UpdatedAt
            };

            await _connection.ExecuteAsync(sql, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", user.Id);
            throw new FshException($"Error updating user {user.Id}", ex);
        }
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        try
        {
            await _connection.ExecuteAsync(
                "DELETE FROM users WHERE id = @Id",
                new { Id = userId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            throw new FshException($"Error deleting user {userId}", ex);
        }
    }

    public async Task ResetPasswordAsync(string email, string newPassword)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(newPassword);

        try
        {
            // Hash the password before saving
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
            var normalizedEmail = email.ToLowerInvariant();
            await _connection.ExecuteAsync(
                "UPDATE users SET password_hash = @PasswordHash WHERE email = @Email",
                new { Email = normalizedEmail, PasswordHash = hashedPassword });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for email {Email}", email);
            throw new FshException($"Error resetting password for email {email}", ex);
        }
    }

    // INTERFACE IMPLEMENTATIONS FOR CLEAN ARCHITECTURE

    public async Task<(bool IsValid, AppUser? User)> ValidatePasswordAndGetByTcknAsync(string tckn, string password)
    {
        ArgumentNullException.ThrowIfNull(tckn);
        ArgumentNullException.ThrowIfNull(password);

        try
        {
            var user = await _connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM users WHERE tckn = @Tckn",
                new { Tckn = tckn });

            if (user == null)
                return (false, null);

            if (string.IsNullOrEmpty(user.password_hash))
                return (false, null);

            // Use BCrypt to verify password against hash
            var isValid = BCrypt.Net.BCrypt.Verify(password, user.password_hash);
            return (isValid, isValid ? MapToAppUser(user) : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating password for TCKN {Tckn}", tckn);
            throw new FshException($"Error validating password for TCKN {tckn}", ex);
        }
    }

    public async Task<IReadOnlyList<string>> GetUserRolesAsync(Guid userId)
    {
        try
        {
            var roles = await _connection.QueryAsync<string>(@"
                SELECT r.id 
                FROM user_roles ur 
                INNER JOIN roles r ON ur.role_id = r.id 
                WHERE ur.user_id = @UserId",
                new { UserId = userId });

            return roles.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles for user {UserId}", userId);
            throw new FshException($"Error getting roles for user {userId}", ex);
        }
    }

    public async Task UpdatePasswordAsync(Guid userId, string hashedPassword)
    {
        ArgumentNullException.ThrowIfNull(hashedPassword);

        try
        {
            await _connection.ExecuteAsync(
                "UPDATE users SET password_hash = @PasswordHash WHERE id = @Id",
                new { Id = userId, PasswordHash = hashedPassword });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating password for user {UserId}", userId);
            throw new FshException($"Error updating password for user {userId}", ex);
        }
    }

    public async Task<bool> ValidateCurrentPasswordAsync(Guid userId, string currentPassword)
    {
        ArgumentNullException.ThrowIfNull(currentPassword);

        try
        {
            var passwordHash = await _connection.QueryFirstOrDefaultAsync<string>(
                "SELECT password_hash FROM users WHERE id = @Id",
                new { Id = userId });

            if (string.IsNullOrEmpty(passwordHash))
                return false;

            return BCrypt.Net.BCrypt.Verify(currentPassword, passwordHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating current password for user {UserId}", userId);
            throw new FshException($"Error validating current password for user {userId}", ex);
        }
    }

    public async Task<bool> TcKimlikExistsAsync(string tcKimlik)
    {
        ArgumentNullException.ThrowIfNull(tcKimlik);

        try
        {
            var count = await _connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM users WHERE tckn = @Tckn",
                new { Tckn = tcKimlik });

            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if TCKN exists {Tckn}", tcKimlik);
            throw new FshException($"Error checking if TCKN exists {tcKimlik}", ex);
        }
    }

    public async Task<(bool IsValid, AppUser? User)> ValidateTcKimlikAndPhoneAsync(string tcKimlik, string phoneNumber)
    {
        ArgumentNullException.ThrowIfNull(tcKimlik);
        ArgumentNullException.ThrowIfNull(phoneNumber);

        try
        {
            var user = await _connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM users WHERE tckn = @Tckn AND phone_number = @PhoneNumber",
                new { Tckn = tcKimlik, PhoneNumber = phoneNumber });

            if (user == null)
                return (false, null);

            return (true, MapToAppUser(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating TCKN and phone number {Tckn}, {PhoneNumber}", tcKimlik, phoneNumber);
            throw new FshException($"Error validating TCKN and phone number {tcKimlik}, {phoneNumber}", ex);
        }
    }

    public async Task UpdateProfileAsync(Guid userId, string? username, string? profession)
    {
        try
        {
            var sql = "UPDATE users SET";
            var parameters = new DynamicParameters();
            parameters.Add("@Id", userId);

            if (username != null)
            {
                sql += " username = @Username,";
                parameters.Add("@Username", username);
            }

            if (profession != null)
            {
                sql += " profession = @Profession,";
                parameters.Add("@Profession", profession);
            }

            sql = sql.TrimEnd(',') + " WHERE id = @Id";
            await _connection.ExecuteAsync(sql, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
            throw new FshException($"Error updating profile for user {userId}", ex);
        }
    }

    public async Task<IReadOnlyList<AppUser>> GetAllAsync()
    {
        try
        {
            var users = await _connection.QueryAsync<dynamic>("SELECT * FROM users");
            return users.Select(MapToAppUser).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            throw new FshException("Error getting all users", ex);
        }
    }

    public async Task AssignRoleAsync(Guid userId, string role)
    {
        ArgumentNullException.ThrowIfNull(role);

        try
        {
            await _connection.ExecuteAsync(
                "INSERT INTO user_roles (user_id, role_id) VALUES (@UserId, @RoleId)",
                new { UserId = userId, RoleId = role });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {Role} to user {UserId}", role, userId);
            throw new FshException($"Error assigning role {role} to user {userId}", ex);
        }
    }

    private static AppUser MapToAppUser(dynamic user)
    {
        return AppUser.FromRepository(
            id: user.id,
            email: user.email,
            username: user.username,
            tckn: user.tckn,
            firstName: user.first_name,
            lastName: user.last_name,
            phoneNumber: user.phone_number,
            profession: user.profession,
            birthDate: user.birth_date)
        .WithPasswordHash(user.password_hash)
        .WithVerificationStatus(
            isIdentityVerified: user.is_identity_verified,
            isPhoneVerified: user.is_phone_verified,
            isEmailVerified: user.is_email_verified)
        .WithStatus(user.status)
        .WithTimestamps(user.created_at, user.updated_at);
    }
} 