using System.Text.RegularExpressions;
using FSH.Framework.Core.Domain.ValueObjects;
using FSH.Framework.Core.Common.Models;

namespace FSH.Framework.Core.Auth.Domain.ValueObjects;

public sealed class Tckn : ValueObject
{
    public string Value { get; }

    private Tckn(string value)
    {
        Value = value;
    }

    public static Result<Tckn> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Tckn>.Failure("TCKN cannot be empty");

        if (value.Length != 11)
            return Result<Tckn>.Failure("TCKN must be 11 digits");

        if (!Regex.IsMatch(value, "^[0-9]*$"))
            return Result<Tckn>.Failure("TCKN must contain only digits");

        // TCKN algoritma kontrolü
        if (!IsValidTckn(value))
            return Result<Tckn>.Failure("Invalid TCKN");

        return Result<Tckn>.Success(new Tckn(value));
    }

    public static Tckn CreateUnsafe(string value)
    {
        if (!IsValid(value))
        {
            throw new ArgumentException("Geçersiz TC Kimlik No", nameof(value));
        }

        return new Tckn(value);
    }

    public static bool IsValid(string tckn)
    {
        if (string.IsNullOrWhiteSpace(tckn))
            return false;

        if (tckn.Length != 11)
            return false;

        if (!Regex.IsMatch(tckn, "^[0-9]*$"))
            return false;

        return IsValidTckn(tckn);
    }

    private static bool IsValidTckn(string tckn)
    {
        if (tckn.Length != 11) return false;

        var digits = tckn.Select(c => c - '0').ToArray();

        // İlk hane 0 olamaz
        if (digits[0] == 0) return false;

        // 1, 3, 5, 7, 9. hanelerin toplamının 7 katından, 2, 4, 6, 8. hanelerin toplamı çıkartıldığında,
        // elde edilen sonucun 10'a bölümünden kalan, yani Mod10'u bize 10. haneyi verir.
        var odd = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        var even = digits[1] + digits[3] + digits[5] + digits[7];
        var digit10 = ((odd * 7) - even) % 10;
        if (digit10 != digits[9]) return false;

        // İlk 10 hanenin toplamından elde edilen sonucun 10'a bölümünden kalan, yani Mod10'u bize 11. haneyi verir.
        var sum = digits.Take(10).Sum();
        var digit11 = sum % 10;
        if (digit11 != digits[10]) return false;

        return true;
    }

    public static implicit operator string(Tckn tckn) => tckn.Value;

    public override string ToString() => Value;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
} 