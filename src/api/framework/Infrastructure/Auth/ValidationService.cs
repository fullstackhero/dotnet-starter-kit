using System.Globalization;
using System.Text.RegularExpressions;
using FSH.Framework.Core.Auth.Services;

namespace FSH.Framework.Infrastructure.Auth;

public sealed class ValidationService : IValidationService
{
    private static readonly HashSet<char> SpecialCharacters = new("!@#$%^&*()_+[]{}|;:,.<>?");

    public bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            // Normalize the domain
            email = Regex.Replace(
                email,
                @"(@)(.+)$", 
                DomainMapper,
                RegexOptions.None | RegexOptions.ExplicitCapture, 
                TimeSpan.FromMilliseconds(200));

            // Examines the domain part of the email and normalizes it.
            static string DomainMapper(Match match)
            {
                // Use IdnMapping class to convert Unicode domain names.
                var idn = new IdnMapping();

                // Pull out and process domain name (throws ArgumentException on invalid)
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

    public bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return false;
        }

        // Turkish phone number pattern: +90 followed by 10 digits
        const string pattern = @"^\+90[0-9]{10}$";
        return Regex.IsMatch(phoneNumber, pattern, RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(250));
    }

    public bool IsValidTCKN(string tckn)
    {
        if (string.IsNullOrWhiteSpace(tckn) || tckn.Length != 11 || !tckn.All(char.IsDigit) || tckn[0] == '0')
        {
            return false;
        }

        var digits = tckn.Select(c => int.Parse(c.ToString(), CultureInfo.InvariantCulture)).ToArray();

        var sumOdd = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        var sumEven = digits[1] + digits[3] + digits[5] + digits[7];

        var check1 = ((sumOdd * 7) - sumEven) % 10;
        var check2 = (sumOdd + sumEven + check1) % 10;

        return check1 == digits[9] && check2 == digits[10];
    }

    public bool IsValidUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        if (username.Length < 3 || username.Length > 20)
        {
            return false;
        }

        // Username can contain letters, numbers, and underscores
        const string pattern = @"^[a-zA-Z0-9_]+$";
        return Regex.IsMatch(username, pattern, RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(250));
    }

    public (bool IsValid, string ErrorMessage) ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return (false, "Password is required");
        }

        if (password.Length < 8)
        {
            return (false, "Password must be at least 8 characters long");
        }

        if (!password.Any(char.IsUpper))
        {
            return (false, "Password must contain at least one uppercase letter");
        }

        if (!password.Any(char.IsLower))
        {
            return (false, "Password must contain at least one lowercase letter");
        }

        if (!password.Any(char.IsDigit))
        {
            return (false, "Password must contain at least one number");
        }

        if (!password.Any(c => SpecialCharacters.Contains(c)))
        {
            return (false, "Password must contain at least one special character");
        }

        return (true, string.Empty);
    }
}