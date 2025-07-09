using System.Globalization;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
using FSH.Framework.Core.Domain.ValueObjects;
using FSH.Framework.Core.Common.Models;

namespace FSH.Framework.Core.Auth.Domain.ValueObjects;

public sealed class Email : ValueObject
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Result<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<Email>.Failure("Email cannot be empty");
        }

        string normalizedEmail = value.Trim();
        try
        {
            // Normalize the domain (IDN) - exception fırlarsa orijinal domaini kullan
            normalizedEmail = Regex.Replace(
                normalizedEmail,
                @"(@)(.+)$",
                match => {
                    try {
                        var idn = new IdnMapping();
                        string domainName = idn.GetAscii(match.Groups[2].Value);
                        return match.Groups[1].Value + domainName;
                    } catch (Exception ex) {
                        // Exception swallow is intentional for IDN domain normalization fallback
                        // Log the full exception for better debugging
                        System.Diagnostics.Debug.WriteLine($"[Email] Domain normalization failed: {ex}");
                        return match.Groups[0].Value;
                    }
                },
                RegexOptions.None | RegexOptions.ExplicitCapture,
                TimeSpan.FromMilliseconds(200));
        }
        catch (RegexMatchTimeoutException)
        {
            return Result<Email>.Failure("Email validation timed out");
        }
        catch (ArgumentException)
        {
            return Result<Email>.Failure("Invalid email format");
        }

        try
        {
            if (!Regex.IsMatch(
                normalizedEmail,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
                TimeSpan.FromMilliseconds(250)))
            {
                return Result<Email>.Failure("Invalid email format");
            }

            // Email normalization: büyük harfli olarak sakla (test ve domain uyumu için)
            normalizedEmail = normalizedEmail.ToUpperInvariant();
            return Result<Email>.Success(new Email(normalizedEmail));
        }
        catch (RegexMatchTimeoutException)
        {
            return Result<Email>.Failure("Email validation timed out");
        }
    }

    public static Email CreateUnsafe(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        value = value.Trim().ToUpperInvariant();
        if (!IsValid(value))
        {
            throw new ArgumentException("Geçersiz email adresi", nameof(value));
        }
        return new Email(value);
    }

    public static bool IsValid(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        try
        {
            email = Regex.Replace(
                email,
                @"(@)(.+)$",
                DomainMapper,
                RegexOptions.None | RegexOptions.ExplicitCapture,
                TimeSpan.FromMilliseconds(200));
            static string DomainMapper(Match match)
            {
                var idn = new IdnMapping();
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
                RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
                TimeSpan.FromMilliseconds(250));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    public static implicit operator string(Email email) => email.Value;

    public override string ToString() => Value;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}