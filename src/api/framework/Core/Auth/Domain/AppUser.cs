using System;
using System.ComponentModel.DataAnnotations;
using FSH.Framework.Core.Common.Models;
using FSH.Framework.Core.Domain;
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
    public int? ProfessionId { get; private init; }
    public DateTime BirthDate { get; private init; }
    public string? MemberNumber { get; private init; }
    public bool IsEmailVerified { get; private init; }
    public bool MarketingConsent { get; private init; }
    public bool ElectronicCommunicationConsent { get; private init; }
    public bool MembershipAgreementConsent { get; private init; }
    public string Status { get; private init; } = default!;
    public string? RegistrationIp { get; private init; }
    public DateTime CreatedAt { get; private init; }
    public DateTime UpdatedAt { get; private init; }

    private AppUser() { }

    // Factory method for creating new users during registration
    public static Result<AppUser> Create(
        string email,
        string username,
        string phoneNumber,
        string tckn,
        string firstName,
        string lastName,
        int? professionId,
        DateTime birthDate,
        bool marketingConsent = false,
        bool electronicCommunicationConsent = false,
        bool membershipAgreementConsent = false,
        string? registrationIp = null)
    {
        // Validation
        var emailResult = EmailVO.Create(email);
        if (!emailResult.IsSuccess)
            return Result<AppUser>.Failure($"Email error: {emailResult.Error}");

        var phoneResult = PhoneNumberVO.Create(phoneNumber);
        if (!phoneResult.IsSuccess)
            return Result<AppUser>.Failure($"Phone error: {phoneResult.Error}");

        var tcknResult = TcknVO.Create(tckn);
        if (!tcknResult.IsSuccess)
            return Result<AppUser>.Failure($"TCKN error: {tcknResult.Error}");

        if (string.IsNullOrWhiteSpace(firstName))
            return Result<AppUser>.Failure("Ad boş olamaz");

        if (string.IsNullOrWhiteSpace(lastName))
            return Result<AppUser>.Failure("Soyad boş olamaz");

        if (string.IsNullOrWhiteSpace(username))
            return Result<AppUser>.Failure("Kullanıcı adı boş olamaz");

        if (birthDate >= DateTime.Today)
            return Result<AppUser>.Failure("Doğum tarihi geçerli olmalıdır");

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = emailResult.Value!,
            Username = username,
            PhoneNumber = phoneResult.Value!,
            Tckn = tcknResult.Value!,
            PasswordHash = string.Empty, // Will be set later
            FirstName = firstName,
            LastName = lastName,
            ProfessionId = professionId,
            BirthDate = birthDate,
            MemberNumber = null, // Will be generated later
            IsEmailVerified = false, // Email verification happens after registration
            MarketingConsent = marketingConsent,
            ElectronicCommunicationConsent = electronicCommunicationConsent,
            MembershipAgreementConsent = membershipAgreementConsent,
            Status = "ACTIVE",
            RegistrationIp = registrationIp,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return Result<AppUser>.Success(user);
    }

    // Factory method for hydrating from repository
    public static AppUser FromRepository(
        Guid id,
        string email,
        string username,
        string phoneNumber,
        string tckn,
        string passwordHash,
        string firstName,
        string lastName,
        int? professionId,
        DateTime birthDate,
        string? memberNumber,
        bool isEmailVerified,
        bool marketingConsent,
        bool electronicCommunicationConsent,
        bool membershipAgreementConsent,
        string status,
        string? registrationIp,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new AppUser
        {
            Id = id,
            Email = EmailVO.Create(email).Value!, // Assume valid from DB
            Username = username,
            PhoneNumber = PhoneNumberVO.Create(phoneNumber).Value!, // Assume valid from DB
            Tckn = TcknVO.Create(tckn).Value!, // Assume valid from DB
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            ProfessionId = professionId,
            BirthDate = birthDate,
            MemberNumber = memberNumber,
            IsEmailVerified = isEmailVerified,
            MarketingConsent = marketingConsent,
            ElectronicCommunicationConsent = electronicCommunicationConsent,
            MembershipAgreementConsent = membershipAgreementConsent,
            Status = status,
            RegistrationIp = registrationIp,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    // Builder pattern methods for fluent configuration
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
            ProfessionId = ProfessionId,
            BirthDate = BirthDate,
            MemberNumber = MemberNumber,
            IsEmailVerified = IsEmailVerified,
            MarketingConsent = MarketingConsent,
            ElectronicCommunicationConsent = ElectronicCommunicationConsent,
            MembershipAgreementConsent = MembershipAgreementConsent,
            Status = Status,
            RegistrationIp = RegistrationIp,
            CreatedAt = CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public AppUser WithEmailVerificationStatus(bool isEmailVerified)
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
            ProfessionId = ProfessionId,
            BirthDate = BirthDate,
            MemberNumber = MemberNumber,
            IsEmailVerified = isEmailVerified,
            MarketingConsent = MarketingConsent,
            ElectronicCommunicationConsent = ElectronicCommunicationConsent,
            MembershipAgreementConsent = MembershipAgreementConsent,
            Status = Status,
            RegistrationIp = RegistrationIp,
            CreatedAt = CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public AppUser WithStatus(string status)
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
            ProfessionId = ProfessionId,
            BirthDate = BirthDate,
            MemberNumber = MemberNumber,
            IsEmailVerified = IsEmailVerified,
            MarketingConsent = MarketingConsent,
            ElectronicCommunicationConsent = ElectronicCommunicationConsent,
            MembershipAgreementConsent = MembershipAgreementConsent,
            Status = status,
            RegistrationIp = RegistrationIp,
            CreatedAt = CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public AppUser WithMemberNumber(string memberNumber)
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
            ProfessionId = ProfessionId,
            BirthDate = BirthDate,
            MemberNumber = memberNumber,
            IsEmailVerified = IsEmailVerified,
            MarketingConsent = MarketingConsent,
            ElectronicCommunicationConsent = ElectronicCommunicationConsent,
            MembershipAgreementConsent = MembershipAgreementConsent,
            Status = Status,
            RegistrationIp = RegistrationIp,
            CreatedAt = CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public AppUser WithProfessionId(int? professionId)
    {
        return new AppUser
        {
            Id = this.Id,
            Email = this.Email,
            Username = this.Username,
            PhoneNumber = this.PhoneNumber,
            Tckn = this.Tckn,
            PasswordHash = this.PasswordHash,
            FirstName = this.FirstName,
            LastName = this.LastName,
            ProfessionId = professionId,
            BirthDate = this.BirthDate,
            MemberNumber = this.MemberNumber,
            IsEmailVerified = this.IsEmailVerified,
            MarketingConsent = this.MarketingConsent,
            ElectronicCommunicationConsent = this.ElectronicCommunicationConsent,
            MembershipAgreementConsent = this.MembershipAgreementConsent,
            Status = this.Status,
            RegistrationIp = this.RegistrationIp,
            CreatedAt = this.CreatedAt,
            UpdatedAt = this.UpdatedAt
        };
    }

    // Business logic methods
    public AppUser SetPassword(string password)
    {
        // Hash the password using BCrypt
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
        
        return new AppUser
        {
            Id = Id,
            Email = Email,
            Username = Username,
            PhoneNumber = PhoneNumber,
            Tckn = Tckn,
            PasswordHash = hashedPassword,
            FirstName = FirstName,
            LastName = LastName,
            ProfessionId = ProfessionId,
            BirthDate = BirthDate,
            MemberNumber = MemberNumber,
            IsEmailVerified = IsEmailVerified,
            MarketingConsent = MarketingConsent,
            ElectronicCommunicationConsent = ElectronicCommunicationConsent,
            MembershipAgreementConsent = MembershipAgreementConsent,
            Status = Status,
            RegistrationIp = RegistrationIp,
            CreatedAt = CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public bool VerifyPassword(string password)
    {
        return !string.IsNullOrEmpty(PasswordHash) && BCrypt.Net.BCrypt.Verify(password, PasswordHash);
    }

    public AppUser UpdateEmailVerificationStatus(bool isEmailVerified)
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
            ProfessionId = ProfessionId,
            BirthDate = BirthDate,
            MemberNumber = MemberNumber,
            IsEmailVerified = isEmailVerified,
            MarketingConsent = MarketingConsent,
            ElectronicCommunicationConsent = ElectronicCommunicationConsent,
            MembershipAgreementConsent = MembershipAgreementConsent,
            Status = Status,
            RegistrationIp = RegistrationIp,
            CreatedAt = CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public AppUser UpdateStatus(string status)
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
            ProfessionId = ProfessionId,
            BirthDate = BirthDate,
            MemberNumber = MemberNumber,
            IsEmailVerified = IsEmailVerified,
            MarketingConsent = MarketingConsent,
            ElectronicCommunicationConsent = ElectronicCommunicationConsent,
            MembershipAgreementConsent = MembershipAgreementConsent,
            Status = status,
            RegistrationIp = RegistrationIp,
            CreatedAt = CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public AppUser UpdateProfile(string firstName, string lastName)
    {
        return new AppUser
        {
            Id = Id,
            Email = Email,
            Username = Username,
            PhoneNumber = PhoneNumber,
            Tckn = Tckn,
            PasswordHash = PasswordHash,
            FirstName = firstName,
            LastName = lastName,
            ProfessionId = ProfessionId,
            BirthDate = BirthDate,
            MemberNumber = MemberNumber,
            IsEmailVerified = IsEmailVerified,
            MarketingConsent = MarketingConsent,
            ElectronicCommunicationConsent = ElectronicCommunicationConsent,
            MembershipAgreementConsent = MembershipAgreementConsent,
            Status = Status,
            RegistrationIp = RegistrationIp,
            CreatedAt = CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public AppUser UpdateEmail(string email)
    {
        var emailResult = EmailVO.Create(email);
        if (!emailResult.IsSuccess)
            throw new ArgumentException($"Invalid email: {emailResult.Error}", nameof(email));

        return new AppUser
        {
            Id = Id,
            Email = emailResult.Value!,
            Username = Username,
            PhoneNumber = PhoneNumber,
            Tckn = Tckn,
            PasswordHash = PasswordHash,
            FirstName = FirstName,
            LastName = LastName,
            ProfessionId = ProfessionId,
            BirthDate = BirthDate,
            MemberNumber = MemberNumber,
            IsEmailVerified = false, // Reset verification when email changes
            MarketingConsent = MarketingConsent,
            ElectronicCommunicationConsent = ElectronicCommunicationConsent,
            MembershipAgreementConsent = MembershipAgreementConsent,
            Status = Status,
            RegistrationIp = RegistrationIp,
            CreatedAt = CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public AppUser UpdatePhoneNumber(string phoneNumber)
    {
        var phoneResult = PhoneNumberVO.Create(phoneNumber);
        if (!phoneResult.IsSuccess)
            throw new ArgumentException($"Invalid phone number: {phoneResult.Error}", nameof(phoneNumber));

        return new AppUser
        {
            Id = Id,
            Email = Email,
            Username = Username,
            PhoneNumber = phoneResult.Value!,
            Tckn = Tckn,
            PasswordHash = PasswordHash,
            FirstName = FirstName,
            LastName = LastName,
            ProfessionId = ProfessionId,
            BirthDate = BirthDate,
            MemberNumber = MemberNumber,
            IsEmailVerified = IsEmailVerified,
            MarketingConsent = MarketingConsent,
            ElectronicCommunicationConsent = ElectronicCommunicationConsent,
            MembershipAgreementConsent = MembershipAgreementConsent,
            Status = Status,
            RegistrationIp = RegistrationIp,
            CreatedAt = CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }
}