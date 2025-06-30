using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using FSH.Framework.Core.Domain.ValueObjects;
using FSH.Framework.Core.Common.Models;

namespace FSH.Framework.Core.Auth.Domain.ValueObjects;

public sealed class Username : ValueObject
{
    public string Value { get; }
    
    private Username(string value) => Value = value;
    
    public static Result<Username> Create(string value)
    {
        if (string.IsNullOrEmpty(value))
            return Result<Username>.Failure("Username cannot be empty");
            
        if (value.Length < 3 || value.Length > 50)
            return Result<Username>.Failure("Username must be between 3 and 50 characters");
            
        if (!Regex.IsMatch(value, @"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(250)))
            return Result<Username>.Failure("Username can only contain letters, numbers, underscore and dash");
            
        return Result<Username>.Success(new Username(value));
    }

    public static bool IsValid(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;
            
        if (value.Length < 3 || value.Length > 50)
            return false;
            
        return Regex.IsMatch(value, @"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(250));
    }

    public static implicit operator string(Username username) => username.Value;

    public override string ToString() => Value;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}