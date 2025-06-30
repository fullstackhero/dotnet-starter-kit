using System;
using System.Text.RegularExpressions;
using FSH.Framework.Core.Domain.ValueObjects;
using FSH.Framework.Core.Common.Models;
using System.Collections.Generic;

namespace FSH.Framework.Core.Auth.Domain.ValueObjects;

public sealed class Name : ValueObject
{
    public string Value { get; }
    
    private Name(string value)
    {
        Value = value;
    }
    
    public static Result<Name> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<Name>.Failure("Name cannot be empty");
        }
        value = value.Trim();
        if (value.Length > 0 && (value[0] == ' ' || value[value.Length - 1] == ' '))
        {
            return Result<Name>.Failure("Name cannot start or end with spaces");
        }
        if (value.Length < 2 || value.Length > 50)
        {
            return Result<Name>.Failure("Name must be between 2 and 50 characters");
        }
        if (!Regex.IsMatch(value, @"^[a-zA-ZğüşıöçĞÜŞİÖÇ\s-]+$", RegexOptions.None | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(200)))
        {
            return Result<Name>.Failure("Name can only contain letters, spaces and dash");
        }
        if (Regex.IsMatch(value, @"\s{2,}", RegexOptions.None | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(200)))
        {
            return Result<Name>.Failure("Name cannot contain multiple consecutive spaces");
        }
        return Result<Name>.Success(new Name(value));
    }

    public static bool IsValid(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        value = value.Trim();
        if (value.Length < 2 || value.Length > 50)
            return false;
        if (!Regex.IsMatch(value, @"^[a-zA-ZğüşıöçĞÜŞİÖÇ\s-]+$", RegexOptions.None | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(200)))
            return false;
        if (Regex.IsMatch(value, @"\s{2,}", RegexOptions.None | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(200)))
            return false;
        return true;
    }

    public static implicit operator string(Name name) => name.Value;

    public override string ToString() => Value;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}