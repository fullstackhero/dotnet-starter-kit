using System;
using System.Text.RegularExpressions;
using FSH.Framework.Core.Domain.ValueObjects;
using FSH.Framework.Core.Common.Models;
using System.Collections.Generic;

namespace FSH.Framework.Core.Auth.Domain.ValueObjects;

public sealed class PhoneNumber : ValueObject
{
    public string Value { get; }

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public static Result<PhoneNumber> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<PhoneNumber>.Failure("Phone number cannot be empty");

        // Remove any non-digit characters for validation
        var digitsOnly = Regex.Replace(value, @"[^\d]", "", RegexOptions.None, TimeSpan.FromMilliseconds(200));

        // Turkish phone number validation: Must be 10 digits starting with 5
        if (digitsOnly.Length != 10)
            return Result<PhoneNumber>.Failure("Phone number must be 10 digits");

        if (digitsOnly.Length > 0 && digitsOnly[0] != '5')
            return Result<PhoneNumber>.Failure("Phone number must start with 5");

        // Normalize phone number (store as formatted)
        var normalized = NormalizePhoneNumber(value);
        
        return Result<PhoneNumber>.Success(new PhoneNumber(normalized));
    }

    public static PhoneNumber CreateUnsafe(string value)
    {
        if (!IsValid(value))
        {
            throw new ArgumentException("Geçersiz telefon numarası", nameof(value));
        }

        return new PhoneNumber(NormalizePhoneNumber(value));
    }

    public static bool IsValid(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        // Remove any non-digit characters for validation
        var digitsOnly = Regex.Replace(phoneNumber, @"[^\d]", "", RegexOptions.None, TimeSpan.FromMilliseconds(200));

        // Turkish phone number validation: Must be 10 digits starting with 5
        return digitsOnly.Length == 10 && digitsOnly[0] == '5';
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        // Remove any non-digit characters and store without formatting
        var digitsOnly = Regex.Replace(phoneNumber, @"[^\d]", "", RegexOptions.None, TimeSpan.FromMilliseconds(200));
        
        // Store as digits only: 5XXXXXXXXX
        if (digitsOnly.Length == 10 && digitsOnly[0] == '5')
        {
            return digitsOnly;
        }
        
        return phoneNumber; // Return as-is if cannot format
    }

    // Method for display formatting (can be used in UI)
    public string ToDisplayFormat()
    {
        var digitsOnly = Regex.Replace(Value, @"[^\d]", "", RegexOptions.None, TimeSpan.FromMilliseconds(200));
        
        // Format as: 5XX XXX XX XX for display
        if (digitsOnly.Length == 10 && digitsOnly[0] == '5')
        {
            return $"{digitsOnly[0]}{digitsOnly[1]}{digitsOnly[2]} {digitsOnly[3]}{digitsOnly[4]}{digitsOnly[5]} {digitsOnly[6]}{digitsOnly[7]} {digitsOnly[8]}{digitsOnly[9]}";
        }
        
        return Value; // Return as-is if cannot format
    }

    public static implicit operator string(PhoneNumber phoneNumber) => phoneNumber.Value;

    public override string ToString() => Value;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}