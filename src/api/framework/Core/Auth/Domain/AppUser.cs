using FSH.Framework.Core.Common.Models;
using EmailVO = FSH.Framework.Core.Auth.Domain.ValueObjects.Email;
using PhoneNumberVO = FSH.Framework.Core.Auth.Domain.ValueObjects.PhoneNumber;
using TcknVO = FSH.Framework.Core.Auth.Domain.ValueObjects.Tckn;

namespace FSH.Framework.Core.Auth.Domain;

public sealed class AppUser
{
    public Guid Id { get; private init; }
    public EmailVO Email { get; private init; } = default!;
    public string Username { get; private init; } = default!;
    public PhoneNumberVO PhoneNumber { get; private init; } = default!;
    public TcknVO Tckn { get; private init; } = default!;
    public string PasswordHash { get; private init; } = default!;
    public string FirstName { get; private init; } = default!;
    public string LastName { get; private init; } = default!;
    public string? Profession { get; private init; }
    public DateTime BirthDate { get; private init; }
    public bool IsIdentityVerified { get; private init; }
    public bool IsPhoneVerified { get; private init; }
    public bool IsEmailVerified { get; private init; }
    public string Status { get; private init; } = default!;
    public DateTime CreatedAt { get; private init; }
    public DateTime UpdatedAt { get; private init; }

    private AppUser() { }

    // Domain factory method for new users
    public static Result<AppUser> Create(
        string email,
        string username,
        string phoneNumber,
        string tckn,
        string firstName,
        string lastName,
        string profession,
        DateTime birthDate)
    {
        try
        {
            var emailResult = EmailVO.Create(email);
            if (!emailResult.IsSuccess)
                return Result<AppUser>.Failure($"Email error: {emailResult.Error}");

            var phoneResult = PhoneNumberVO.Create(phoneNumber);
            if (!phoneResult.IsSuccess)
                return Result<AppUser>.Failure($"Phone error: {phoneResult.Error}");

            var tcknResult = TcknVO.Create(tckn);
            if (!tcknResult.IsSuccess)
                return Result<AppUser>.Failure($"TCKN error: {tcknResult.Error}");

            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                Email = emailResult.Value!,
                Username = username,
                PhoneNumber = phoneResult.Value!,
                Tckn = tcknResult.Value!,
                FirstName = firstName,
                LastName = lastName,
                Profession = profession,
                BirthDate = birthDate,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return Result<AppUser>.Success(user);
        }
        catch (ArgumentException ex)
        {
            return Result<AppUser>.Failure(ex.Message);
        }
    }

    // Domain factory method for existing users from repository
    public static AppUser FromRepository(
        Guid id,
        string email,
        string username,
        string tckn,
        string firstName,
        string lastName,
        string? phoneNumber,
        string? profession,
        DateTime? birthDate)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(username);
        ArgumentNullException.ThrowIfNull(tckn);
        ArgumentNullException.ThrowIfNull(firstName);
        ArgumentNullException.ThrowIfNull(lastName);

        var emailVO = EmailVO.CreateUnsafe(email);
        var tcknVO = TcknVO.CreateUnsafe(tckn);
        var phoneVO = string.IsNullOrEmpty(phoneNumber) 
            ? PhoneNumberVO.CreateUnsafe("5000000000") // Default phone for backward compatibility
            : PhoneNumberVO.CreateUnsafe(phoneNumber);

        return new AppUser
        {
            Id = id,
            Email = emailVO,
            Username = username,
            PhoneNumber = phoneVO,
            Tckn = tcknVO,
            FirstName = firstName,
            LastName = lastName,
            Profession = profession,
            BirthDate = birthDate ?? DateTime.MinValue,
            Status = "ACTIVE", // Default status for backward compatibility
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // Fluent builder methods for repository mapping
    public AppUser WithPasswordHash(string passwordHash)
    {
        return new AppUser
        {
            Id = Id,
            Email = Email,
            Username = Username,
            PhoneNumber = PhoneNumber,
            Tckn = Tckn,
            PasswordHash = passwordHash,
            FirstName = FirstName,
            LastName = LastName,
            Profession = Profession,
            BirthDate = BirthDate,
            IsIdentityVerified = IsIdentityVerified,
            IsPhoneVerified = IsPhoneVerified,
            IsEmailVerified = IsEmailVerified,
            Status = Status,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
    }

    public AppUser WithVerificationStatus(bool isIdentityVerified, bool isPhoneVerified, bool isEmailVerified)
    {
        return new AppUser
        {
            Id = Id,
            Email = Email,
            Username = Username,
            PhoneNumber = PhoneNumber,
            Tckn = Tckn,
            PasswordHash = PasswordHash,
            FirstName = FirstName,
            LastName = LastName,
            Profession = Profession,
            BirthDate = BirthDate,
            IsIdentityVerified = isIdentityVerified,
            IsPhoneVerified = isPhoneVerified,
            IsEmailVerified = isEmailVerified,
            Status = Status,
            CreatedAt = CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public AppUser WithStatus(string status)
    {
        ArgumentNullException.ThrowIfNull(status);
        return new AppUser
        {
            Id = Id,
            Email = Email,
            Username = Username,
            PhoneNumber = PhoneNumber,
            Tckn = Tckn,
            PasswordHash = PasswordHash,
            FirstName = FirstName,
            LastName = LastName,
            Profession = Profession,
            BirthDate = BirthDate,
            IsIdentityVerified = IsIdentityVerified,
            IsPhoneVerified = IsPhoneVerified,
            IsEmailVerified = IsEmailVerified,
            Status = status,
            CreatedAt = CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public AppUser WithTimestamps(DateTime createdAt, DateTime updatedAt)
    {
        return new AppUser
        {
            Id = Id,
            Email = Email,
            Username = Username,
            PhoneNumber = PhoneNumber,
            Tckn = Tckn,
            PasswordHash = PasswordHash,
            FirstName = FirstName,
            LastName = LastName,
            Profession = Profession,
            BirthDate = BirthDate,
            IsIdentityVerified = IsIdentityVerified,
            IsPhoneVerified = IsPhoneVerified,
            IsEmailVerified = IsEmailVerified,
            Status = Status,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    // Domain method for password handling
    public AppUser WithPassword(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        return WithPasswordHash(BCrypt.Net.BCrypt.HashPassword(password));
    }

    // Domain method for updating user profile
    public Result<AppUser> UpdateProfile(
        string? email = null,
        string? phoneNumber = null,
        string? username = null,
        string? firstName = null,
        string? lastName = null,
        string? profession = null)
    {
        try
        {
            var updatedUser = this;

            if (email != null)
            {
                var emailResult = EmailVO.Create(email);
                if (!emailResult.IsSuccess)
                    return Result<AppUser>.Failure($"Email error: {emailResult.Error}");

                updatedUser = new AppUser
                {
                    Id = Id,
                    Email = emailResult.Value!,
                    Username = Username,
                    PhoneNumber = PhoneNumber,
                    Tckn = Tckn,
                    PasswordHash = PasswordHash,
                    FirstName = FirstName,
                    LastName = LastName,
                    Profession = Profession,
                    BirthDate = BirthDate,
                    IsIdentityVerified = IsIdentityVerified,
                    IsPhoneVerified = IsPhoneVerified,
                    IsEmailVerified = IsEmailVerified,
                    Status = Status,
                    CreatedAt = CreatedAt,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            if (phoneNumber != null)
            {
                var phoneResult = PhoneNumberVO.Create(phoneNumber);
                if (!phoneResult.IsSuccess)
                    return Result<AppUser>.Failure($"Phone error: {phoneResult.Error}");

                updatedUser = new AppUser
                {
                    Id = updatedUser.Id,
                    Email = updatedUser.Email,
                    Username = updatedUser.Username,
                    PhoneNumber = phoneResult.Value!,
                    Tckn = updatedUser.Tckn,
                    PasswordHash = updatedUser.PasswordHash,
                    FirstName = updatedUser.FirstName,
                    LastName = updatedUser.LastName,
                    Profession = updatedUser.Profession,
                    BirthDate = updatedUser.BirthDate,
                    IsIdentityVerified = updatedUser.IsIdentityVerified,
                    IsPhoneVerified = updatedUser.IsPhoneVerified,
                    IsEmailVerified = updatedUser.IsEmailVerified,
                    Status = updatedUser.Status,
                    CreatedAt = updatedUser.CreatedAt,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            if (username != null || firstName != null || lastName != null || profession != null)
            {
                updatedUser = new AppUser
                {
                    Id = updatedUser.Id,
                    Email = updatedUser.Email,
                    Username = username ?? updatedUser.Username,
                    PhoneNumber = updatedUser.PhoneNumber,
                    Tckn = updatedUser.Tckn,
                    PasswordHash = updatedUser.PasswordHash,
                    FirstName = firstName ?? updatedUser.FirstName,
                    LastName = lastName ?? updatedUser.LastName,
                    Profession = profession ?? updatedUser.Profession,
                    BirthDate = updatedUser.BirthDate,
                    IsIdentityVerified = updatedUser.IsIdentityVerified,
                    IsPhoneVerified = updatedUser.IsPhoneVerified,
                    IsEmailVerified = updatedUser.IsEmailVerified,
                    Status = updatedUser.Status,
                    CreatedAt = updatedUser.CreatedAt,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            return Result<AppUser>.Success(updatedUser);
        }
        catch (ArgumentException ex)
        {
            return Result<AppUser>.Failure(ex.Message);
        }
    }

    // Domain methods for admin operations
    public AppUser UpdateVerificationStatus(bool isIdentityVerified, bool isPhoneVerified, bool isEmailVerified)
    {
        return new AppUser
        {
            Id = Id,
            Email = Email,
            Username = Username,
            PhoneNumber = PhoneNumber,
            Tckn = Tckn,
            PasswordHash = PasswordHash,
            FirstName = FirstName,
            LastName = LastName,
            Profession = Profession,
            BirthDate = BirthDate,
            IsIdentityVerified = isIdentityVerified,
            IsPhoneVerified = isPhoneVerified,
            IsEmailVerified = isEmailVerified,
            Status = Status,
            CreatedAt = CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public AppUser UpdateStatus(string status)
    {
        ArgumentNullException.ThrowIfNull(status);
        return new AppUser
        {
            Id = Id,
            Email = Email,
            Username = Username,
            PhoneNumber = PhoneNumber,
            Tckn = Tckn,
            PasswordHash = PasswordHash,
            FirstName = FirstName,
            LastName = LastName,
            Profession = Profession,
            BirthDate = BirthDate,
            IsIdentityVerified = IsIdentityVerified,
            IsPhoneVerified = IsPhoneVerified,
            IsEmailVerified = IsEmailVerified,
            Status = status,
            CreatedAt = CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public AppUser SetPassword(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        return WithPassword(password);
    }
} 