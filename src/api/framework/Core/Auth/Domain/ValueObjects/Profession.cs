using System.Collections.Generic;
using FSH.Framework.Core.Domain.ValueObjects;
using FSH.Framework.Core.Common.Models;

namespace FSH.Framework.Core.Auth.Domain.ValueObjects;

public sealed class Profession : ValueObject
{
    public string? Value { get; }
    
    private Profession(string? value)
    {
        Value = value;
    }
    
    public static Profession CreateEmpty() => new(null);
    
    public static Result<Profession> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<Profession>.Success(CreateEmpty());
        }

        value = value.Trim();
        
        if (value.Length > 0 && (value[0] == ' ' || value[value.Length - 1] == ' '))
        {
            return Result<Profession>.Failure("Profession cannot start or end with spaces");
        }

        if (value.Length < 2 || value.Length > 100)
        {
            return Result<Profession>.Failure("Profession must be between 2 and 100 characters");
        }

        return Result<Profession>.Success(new Profession(value));
    }

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true; // Empty is valid

        value = value.Trim();
        
        if (value.Length < 2 || value.Length > 100)
            return false;

        return true;
    }

    public static implicit operator string?(Profession profession) => profession.Value;

    public override string ToString() => Value ?? string.Empty;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value ?? string.Empty;
    }
}