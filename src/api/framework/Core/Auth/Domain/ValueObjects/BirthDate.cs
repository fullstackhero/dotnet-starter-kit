using System;
using System.Collections.Generic;
using FSH.Framework.Core.Domain.ValueObjects;
using FSH.Framework.Core.Common.Models;
using System.Globalization;

namespace FSH.Framework.Core.Auth.Domain.ValueObjects;

public sealed class BirthDate : ValueObject
{
    public DateTime Value { get; }
    
    private BirthDate(DateTime value) => Value = value;
    
    public static Result<BirthDate> Create(DateTime? value)
    {
        if (!value.HasValue)
            return Result<BirthDate>.Failure("Birth date is required");
            
        var minDate = DateTime.UtcNow.AddYears(-100);
        var maxDate = DateTime.UtcNow.AddYears(-18);
        
        if (value.Value < minDate || value.Value > maxDate)
            return Result<BirthDate>.Failure("Birth date must be between 18 and 100 years ago");
            
        return Result<BirthDate>.Success(new BirthDate(value.Value));
    }

    public static bool IsValid(DateTime? value)
    {
        if (!value.HasValue)
            return false;
            
        var minDate = DateTime.UtcNow.AddYears(-100);
        var maxDate = DateTime.UtcNow.AddYears(-18);
        
        return value.Value >= minDate && value.Value <= maxDate;
    }

    public static implicit operator DateTime(BirthDate birthDate) => birthDate.Value;
    public DateTime ToDateTime() => Value;
    public override string ToString() => Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}