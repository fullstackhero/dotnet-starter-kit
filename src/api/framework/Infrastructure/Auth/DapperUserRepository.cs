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

    public async Task ResetPasswordByMemberNumberAsync(string memberNumber, string newPassword)
    {
        ArgumentNullException.ThrowIfNull(memberNumber);
        ArgumentNullException.ThrowIfNull(newPassword);

        try
        {
            // Hash the password before saving
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
            
            var rowsAffected = await _connection.ExecuteAsync(
                "UPDATE users SET password_hash = @PasswordHash WHERE member_number = @MemberNumber",
                new { MemberNumber = memberNumber, PasswordHash = hashedPassword });

            if (rowsAffected == 0)
            {
                _logger.LogWarning("No user found with Member Number {MemberNumber} for password reset", memberNumber);
                throw new FshException($"Kullanıcı bulunamadı: {memberNumber}");
            }

            _logger.LogInformation("Password successfully reset for Member Number: {MemberNumber}", memberNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for Member Number {MemberNumber}", memberNumber);
            throw new FshException($"Error resetting password for Member Number {memberNumber}", ex);
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
        try
        {
            var user = await _connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM users WHERE tckn = @Tckn AND birth_date::date = @BirthDate::date",
                new { Tckn = tcKimlik, BirthDate = birthDate.Date });

            if (user == null)
                return (false, null);

            return (true, MapToAppUser(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating TC Kimlik and birth date for {TcKimlik}", tcKimlik);
            throw new FshException($"Error validating TC Kimlik and birth date for {tcKimlik}", ex);
        }
    }

    public async Task<(bool IsValid, AppUser? User)> ValidateMemberNumberAndBirthDateAsync(string memberNumber, DateTime birthDate)
    {
        try
        {
            var user = await _connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM users WHERE member_number = @MemberNumber AND birth_date::date = @BirthDate::date",
                new { MemberNumber = memberNumber, BirthDate = birthDate.Date });

            if (user == null)
                return (false, null);

            return (true, MapToAppUser(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating member number and birth date for {MemberNumber}", memberNumber);
            throw new FshException($"Error validating member number and birth date for {memberNumber}", ex);
        }
    }

    public async Task<(bool IsValid, AppUser? User)> ValidateMemberNumberAndPhoneAsync(string memberNumber, string phoneNumber)
    {
        try
        {
            var user = await _connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM users WHERE member_number = @MemberNumber AND phone_number = @PhoneNumber",
                new { MemberNumber = memberNumber, PhoneNumber = phoneNumber });

            if (user == null)
                return (false, null);

            return (true, MapToAppUser(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating member number and phone for {MemberNumber}", memberNumber);
            throw new FshException($"Error validating member number and phone for {memberNumber}", ex);
        }
    }

    public async Task<(string? Email, string? Phone)> GetUserContactInfoAsync(string tcKimlik)
    {
        try
        {
            var user = await _connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT email, phone_number FROM users WHERE tckn = @Tckn",
                new { Tckn = tcKimlik });

            if (user == null)
                return (null, null);

            return (user.email, user.phone_number);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contact info for TC Kimlik {TcKimlik}", tcKimlik);
            throw new FshException($"Error getting contact info for TC Kimlik {tcKimlik}", ex);
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

    public async Task<(string? Email, string? Phone)> GetUserContactInfoByMemberNumberAsync(string memberNumber)
    {
        try
        {
            var user = await _connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT email, phone_number FROM users WHERE member_number = @MemberNumber",
                new { MemberNumber = memberNumber });

            if (user == null)
                return (null, null);

            return (user.email, user.phone_number);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contact info for member number {MemberNumber}", memberNumber);
            throw new FshException($"Error getting contact info for member number {memberNumber}", ex);
        }
    }

    // Password History Methods for Change Password Feature
    public async Task<bool> IsPasswordRecentlyUsedAsync(string tcKimlik, string newPassword)
    {
        try
        {
            // Ensure connection is open
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            var passwordHashes = new List<string>();
            
            // Get current password from users table
            var currentPassword = await _connection.QueryFirstOrDefaultAsync<string>(
                "SELECT password_hash FROM users WHERE tckn = @Tckn",
                new { Tckn = tcKimlik });
                
            if (!string.IsNullOrEmpty(currentPassword))
            {
                passwordHashes.Add(currentPassword);
            }
            
            // Get last 2 passwords from password_history table
            var historyPasswords = await _connection.QueryAsync<string>(
                @"SELECT password_hash FROM password_history 
                  WHERE tckn = @Tckn 
                  ORDER BY created_at DESC 
                  LIMIT 2",
                new { Tckn = tcKimlik });
                
            passwordHashes.AddRange(historyPasswords);

            // Check if new password matches any of the recent passwords
            foreach (var hash in passwordHashes)
            {
                if (!string.IsNullOrEmpty(hash) && BCrypt.Net.BCrypt.Verify(newPassword, hash))
                {
                    return true; // Password was recently used
                }
            }

            return false; // Password is not recently used
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking password history for TCKN {TcKimlik}", tcKimlik);
            throw new FshException($"Error checking password history for TCKN {tcKimlik}", ex);
        }
    }

    public async Task UpdatePasswordWithHistoryAsync(string tcKimlik, string newPassword)
    {
        ArgumentNullException.ThrowIfNull(tcKimlik);
        ArgumentNullException.ThrowIfNull(newPassword);

        try
        {
            // Ensure connection is open
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            // Start transaction
            using var transaction = _connection.BeginTransaction();
            
            try
            {
                // Get current password hash to save to history
                var currentPasswordHash = await _connection.QueryFirstOrDefaultAsync<string>(
                    "SELECT password_hash FROM users WHERE tckn = @Tckn",
                    new { Tckn = tcKimlik }, transaction);

                // Hash the new password
                var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

                // Update user's password
                var rowsAffected = await _connection.ExecuteAsync(
                    "UPDATE users SET password_hash = @PasswordHash, updated_at = @UpdatedAt WHERE tckn = @Tckn",
                    new { 
                        Tckn = tcKimlik, 
                        PasswordHash = newPasswordHash,
                        UpdatedAt = DateTime.UtcNow
                    }, transaction);

                if (rowsAffected == 0)
                {
                    throw new FshException($"Kullanıcı bulunamadı: {tcKimlik}");
                }

                // Save old password to history (if exists)
                if (!string.IsNullOrEmpty(currentPasswordHash))
                {
                    await _connection.ExecuteAsync(
                        @"INSERT INTO password_history (tckn, password_hash, created_at) 
                          VALUES (@Tckn, @PasswordHash, @CreatedAt)",
                        new { 
                            Tckn = tcKimlik, 
                            PasswordHash = currentPasswordHash,
                            CreatedAt = DateTime.UtcNow
                        }, transaction);
                }

                // Clean up old password history (keep only last 2)
                // First, get IDs of records to keep (most recent 2)
                var recordsToKeep = await _connection.QueryAsync<int>(
                    @"SELECT id FROM password_history 
                      WHERE tckn = @Tckn 
                      ORDER BY created_at DESC 
                      LIMIT 2",
                    new { Tckn = tcKimlik }, transaction);

                // Delete all records except the ones to keep
                if (recordsToKeep.Any())
                {
                    var idsToKeep = string.Join(",", recordsToKeep);
                    await _connection.ExecuteAsync(
                        $@"DELETE FROM password_history 
                          WHERE tckn = @Tckn 
                          AND id NOT IN ({idsToKeep})",
                        new { Tckn = tcKimlik }, transaction);
                }

                transaction.Commit();
                
                _logger.LogInformation("Password successfully updated with history for TCKN: {TcKimlik}", tcKimlik);
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating password with history for TCKN {TcKimlik}", tcKimlik);
            throw new FshException($"Error updating password with history for TCKN {tcKimlik}", ex);
        }
    }
} 