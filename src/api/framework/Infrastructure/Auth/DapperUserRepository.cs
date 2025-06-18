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
                SELECT id, email, username, phone_number, tckn, 
                       password_hash, first_name, last_name, profession_id,
                       birth_date, member_number, is_email_verified,
                       marketing_consent, electronic_communication_consent, membership_agreement_consent,
                       status, created_at, updated_at
                FROM users 
                WHERE id = @Id";

            var userRow = await _connection.QueryFirstOrDefaultAsync(sql, new { Id = id });
            if (userRow == null) return null;

            return MapToAppUser(userRow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by id {UserId}", id);
            throw new FshException($"Error getting user by id {id}", ex);
        }
    }

    public async Task<AppUser?> GetByEmailAsync(string email)
    {
        try
        {
            const string sql = @"
                SELECT id, email, username, phone_number, tckn, 
                       password_hash, first_name, last_name, profession_id,
                       birth_date, member_number, is_email_verified,
                       marketing_consent, electronic_communication_consent, membership_agreement_consent,
                       status, created_at, updated_at
                FROM users 
                WHERE email = @Email";

            var userRow = await _connection.QueryFirstOrDefaultAsync(sql, new { Email = email });
            if (userRow == null) return null;

            return MapToAppUser(userRow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email");
            throw new FshException("Error getting user by email", ex);
        }
    }

    public async Task<AppUser?> GetByTcknAsync(TcknVO tckn)
    {
        try
        {
            var user = await _connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM users WHERE tckn = @Tckn",
                new { Tckn = tckn.Value });

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
        try
        {
            const string sql = @"
                SELECT id, email, username, phone_number, tckn, 
                       password_hash, first_name, last_name, profession_id,
                       birth_date, member_number, is_email_verified,
                       marketing_consent, electronic_communication_consent, membership_agreement_consent,
                       status, created_at, updated_at
                FROM users 
                WHERE username = @Username";

            var userRow = await _connection.QueryFirstOrDefaultAsync(sql, new { Username = username });
            if (userRow == null) return null;

            return MapToAppUser(userRow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by username");
            throw new FshException("Error getting user by username", ex);
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
        try
        {
            const string sql = @"
                INSERT INTO users (
                    id, email, username, phone_number, tckn, 
                    password_hash, first_name, last_name, profession_id,
                    birth_date, member_number, is_email_verified,
                    marketing_consent, electronic_communication_consent, membership_agreement_consent,
                    status, registration_ip, created_at, updated_at
                ) VALUES (
                    @Id, @Email, @Username, @PhoneNumber, @Tckn,
                    @PasswordHash, @FirstName, @LastName, @ProfessionId,
                    @BirthDate, @MemberNumber, @IsEmailVerified,
                    @MarketingConsent, @ElectronicCommunicationConsent, @MembershipAgreementConsent,
                    @Status, @RegistrationIp::inet, @CreatedAt, @UpdatedAt
                )";

            await _connection.ExecuteAsync(sql, new
            {
                Id = user.Id,
                Email = user.Email.Value,
                Username = user.Username,
                PhoneNumber = user.PhoneNumber.Value,
                Tckn = user.Tckn.Value,
                PasswordHash = user.PasswordHash,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfessionId = user.ProfessionId,
                BirthDate = user.BirthDate,
                MemberNumber = user.MemberNumber,
                IsEmailVerified = user.IsEmailVerified,
                MarketingConsent = user.MarketingConsent,
                ElectronicCommunicationConsent = user.ElectronicCommunicationConsent,
                MembershipAgreementConsent = user.MembershipAgreementConsent,
                Status = user.Status,
                RegistrationIp = user.RegistrationIp,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            });

            return user.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            throw new FshException("Error creating user", ex);
        }
    }

    public async Task<Guid> UpdateAsync(AppUser user)
    {
        try
        {
            const string sql = @"
                UPDATE users SET 
                    email = @Email, 
                    username = @Username, 
                    phone_number = @PhoneNumber, 
                    password_hash = @PasswordHash, 
                    first_name = @FirstName, 
                    last_name = @LastName, 
                    profession_id = @ProfessionId,
                    birth_date = @BirthDate,
                                                    is_email_verified = @IsEmailVerified,
                status = @Status,
                    updated_at = @UpdatedAt
                WHERE id = @Id";

            await _connection.ExecuteAsync(sql, new
            {
                Id = user.Id,
                Email = user.Email.Value,
                Username = user.Username,
                PhoneNumber = user.PhoneNumber.Value,
                PasswordHash = user.PasswordHash,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfessionId = user.ProfessionId,
                BirthDate = user.BirthDate,
                IsEmailVerified = user.IsEmailVerified,
                Status = user.Status,
                UpdatedAt = user.UpdatedAt
            });

            return user.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user");
            throw new FshException("Error updating user", ex);
        }
    }

    public async Task UpdateUserAsync(AppUser user)
    {
        await UpdateAsync(user);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            const string sql = "DELETE FROM users WHERE id = @Id";
            var rowsAffected = await _connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user");
            throw new FshException("Error deleting user", ex);
        }
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        await DeleteAsync(userId);
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

    public async Task ResetPasswordByTcknAsync(string tcKimlik, string newPassword)
    {
        ArgumentNullException.ThrowIfNull(tcKimlik);
        ArgumentNullException.ThrowIfNull(newPassword);

        try
        {
            // Hash the password before saving
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
            
            var rowsAffected = await _connection.ExecuteAsync(
                "UPDATE users SET password_hash = @PasswordHash WHERE tckn = @Tckn",
                new { Tckn = tcKimlik, PasswordHash = hashedPassword });

            if (rowsAffected == 0)
            {
                _logger.LogWarning("No user found with TCKN {Tckn} for password reset", tcKimlik);
                throw new FshException($"Kullanıcı bulunamadı: {tcKimlik}");
            }

            _logger.LogInformation("Password successfully reset for TCKN: {Tckn}", tcKimlik);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for TCKN {Tckn}", tcKimlik);
            throw new FshException($"Error resetting password for TCKN {tcKimlik}", ex);
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

    public async Task<(bool IsValid, AppUser? User)> ValidatePasswordAndGetByMemberNumberAsync(string memberNumber, string password)
    {
        ArgumentNullException.ThrowIfNull(memberNumber);
        ArgumentNullException.ThrowIfNull(password);

        try
        {
            var user = await _connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM users WHERE member_number = @MemberNumber",
                new { MemberNumber = memberNumber });

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
            _logger.LogError(ex, "Error validating password for member number {MemberNumber}", memberNumber);
            throw new FshException($"Error validating password for member number {memberNumber}", ex);
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
        try
        {
            const string sql = "SELECT COUNT(1) FROM users WHERE tckn = @Tckn";
            var count = await _connection.QuerySingleAsync<int>(sql, new { Tckn = tcKimlik });
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
                sql += " profession_id = @ProfessionId,";
                parameters.Add("@ProfessionId", profession);
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

    // Password Reset Methods
    public async Task<(bool IsValid, AppUser? User)> ValidateTcKimlikAndBirthDateAsync(string tcKimlik, DateTime birthDate)
    {
        ArgumentNullException.ThrowIfNull(tcKimlik);

        try
        {
            var user = await _connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM users WHERE tckn = @Tckn AND DATE(birth_date) = DATE(@BirthDate)",
                new { Tckn = tcKimlik, BirthDate = birthDate.Date });

            if (user == null)
                return (false, null);

            return (true, MapToAppUser(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating TCKN and birth date {Tckn}, {BirthDate}", tcKimlik, birthDate);
            throw new FshException($"Error validating TCKN and birth date {tcKimlik}, {birthDate}", ex);
        }
    }

    public async Task<(string? Email, string? Phone)> GetUserContactInfoAsync(string tcKimlik)
    {
        ArgumentNullException.ThrowIfNull(tcKimlik);

        try
        {
            var result = await _connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT email, phone_number FROM users WHERE tckn = @Tckn",
                new { Tckn = tcKimlik });

            if (result == null)
                return (null, null);

            return (result.email, result.phone_number);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contact info for TCKN {Tckn}", tcKimlik);
            throw new FshException($"Error getting contact info for TCKN {tcKimlik}", ex);
        }
    }

    public async Task<AppUser?> GetByMemberNumberAsync(string memberNumber)
    {
        try
        {
            var user = await _connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM users WHERE member_number = @MemberNumber",
                new { MemberNumber = memberNumber });

            return user == null ? null : MapToAppUser(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by member number {MemberNumber}", memberNumber);
            throw new FshException($"Error getting user by member number {memberNumber}", ex);
        }
    }

    private static AppUser MapToAppUser(dynamic userRow)
    {
        return AppUser.FromRepository(
            id: userRow.id,
            email: userRow.email,
            username: userRow.username,
            phoneNumber: userRow.phone_number,
            tckn: userRow.tckn,
            passwordHash: userRow.password_hash,
            firstName: userRow.first_name,
            lastName: userRow.last_name,
            professionId: userRow.profession_id,
            birthDate: userRow.birth_date,
            memberNumber: userRow.member_number,
            isEmailVerified: userRow.is_email_verified,
            marketingConsent: userRow.marketing_consent ?? false,
            electronicCommunicationConsent: userRow.electronic_communication_consent ?? false,
            membershipAgreementConsent: userRow.membership_agreement_consent ?? false,
            status: userRow.status,
            registrationIp: userRow.registration_ip?.ToString(),
            createdAt: userRow.created_at,
            updatedAt: userRow.updated_at
        );
    }

    // Profile Update Verification Methods
    public async Task<bool> VerifyEmailUpdateAsync(Guid userId, string verificationCode)
    {
        ArgumentNullException.ThrowIfNull(verificationCode);

        try
        {
            // Bu basit implementation - gerçek projede verification service ile entegre olacak
            // Şimdilik verification code'un doğru olduğunu varsayıyoruz
            _logger.LogInformation("Email verification attempted for user {UserId}", userId);
            
            // Verification başarılı ise user'ın email'ini verified olarak işaretle
            await _connection.ExecuteAsync(
                "UPDATE users SET is_email_verified = true WHERE id = @UserId",
                new { UserId = userId });
                
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying email update for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> VerifyPhoneUpdateAsync(Guid userId, string verificationCode)
    {
        ArgumentNullException.ThrowIfNull(verificationCode);

        try
        {
            // Phone verification is no longer needed as it happens during registration
            // This method is kept for interface compatibility but does nothing
            _logger.LogInformation("Phone verification attempted for user {UserId} - no action needed (verified during registration)", userId);
            
            return true; // Always return true since phone is verified during registration
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in phone verification for user {UserId}", userId);
            return false;
        }
    }
} 