using FSH.Framework.Core.Auth.Domain;
using FSH.Framework.Core.Auth.Domain.ValueObjects;

namespace FSH.Framework.Core.Auth.Repositories;

public interface IUserRepository
{
    Task<(bool IsValid, AppUser? User)> ValidatePasswordAndGetByTcknAsync(string tckn, string password);
    Task<(bool IsValid, AppUser? User)> ValidatePasswordAndGetByMemberNumberAsync(string memberNumber, string password);
    Task<IReadOnlyList<string>> GetUserRolesAsync(Guid userId);
    Task<bool> EmailExistsAsync(string email, Guid? excludeId = null);
    Task<bool> UsernameExistsAsync(string username, Guid? excludeId = null);
    // Added for phone number update validation in profile management
    Task<bool> PhoneExistsAsync(string phoneNumber, Guid? excludeId = null);
    Task<Guid> CreateUserAsync(AppUser user);
    Task<AppUser?> GetByIdAsync(Guid id);
    Task<AppUser?> GetByMemberNumberAsync(string memberNumber);
    Task UpdatePasswordAsync(Guid userId, string hashedPassword);
    Task<bool> ValidateCurrentPasswordAsync(Guid userId, string currentPassword);
    Task<bool> TcKimlikExistsAsync(string tcKimlik);
    Task<(bool IsValid, AppUser? User)> ValidateTcKimlikAndPhoneAsync(string tcKimlik, string phoneNumber);
    Task UpdateProfileAsync(Guid userId, string? username, string? profession);
    Task<IReadOnlyList<AppUser>> GetAllAsync();
    Task UpdateUserAsync(AppUser user);
    Task DeleteUserAsync(Guid userId);
    Task AssignRoleAsync(Guid userId, string role);
    Task<AppUser?> GetByTcknAsync(Tckn tckn);
    
    // Password Reset Methods
    Task<(bool IsValid, AppUser? User)> ValidateTcKimlikAndBirthDateAsync(string tcKimlik, DateTime birthDate);
    Task<(string? Email, string? Phone)> GetUserContactInfoAsync(string tcKimlik);
    Task ResetPasswordAsync(string email, string newPassword);
    Task ResetPasswordByTcknAsync(string tcKimlik, string newPassword);
    
    // Profile Update Verification Methods
    Task<bool> VerifyEmailUpdateAsync(Guid userId, string verificationCode);
    Task<bool> VerifyPhoneUpdateAsync(Guid userId, string verificationCode);
} 