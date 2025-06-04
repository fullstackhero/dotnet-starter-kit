using FSH.Framework.Core.Domain.ValueObjects;
using FSH.Framework.Core.Common.Models;
using System.Text.RegularExpressions;

namespace FSH.Framework.Core.Auth.Domain.ValueObjects;

public sealed class Password : ValueObject
{
    public string Value { get; }

    private Password(string value)
    {
        Value = value;
    }

    public static Result<Password> Create(string value)
    {
        if (string.IsNullOrEmpty(value))
            return Result<Password>.Failure("Password cannot be empty");
            
        if (value.Length < 8)
            return Result<Password>.Failure("Password must be at least 8 characters");
            
        if (!Regex.IsMatch(value, @"[A-Z]"))
            return Result<Password>.Failure("Password must contain at least one uppercase letter");
            
        if (!Regex.IsMatch(value, @"[a-z]"))
            return Result<Password>.Failure("Password must contain at least one lowercase letter");
            
        if (!Regex.IsMatch(value, @"[0-9]"))
            return Result<Password>.Failure("Password must contain at least one number");
            
        if (!Regex.IsMatch(value, @"[!@#$%^&*()_+\[\]{}|;:,.?""'<>-]"))
            return Result<Password>.Failure("Password must contain at least one special character");
            
        return Result<Password>.Success(new Password(value));
    }

    public static Password CreateUnsafe(string value)
    {
        var validation = ValidatePassword(value);
        if (!validation)
        {
            throw new ArgumentException("Geçersiz şifre", nameof(value));
        }

        return new Password(value);
    }

    public static bool ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        return password.Length >= 8 &&
               password.Any(char.IsUpper) &&
               password.Any(char.IsLower) &&
               password.Any(char.IsDigit) &&
               Regex.IsMatch(password, @"[!@#$%^&*()_+\[\]{}|;:,.?""'<>-]");
    }

    public static bool IsValid(string password)
    {
        return ValidatePassword(password);
    }

    public static implicit operator string(Password password) => password.Value;

    public override string ToString() => "***"; // Security: Don't expose password value

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
} 