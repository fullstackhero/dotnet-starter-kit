# Localized DataAnnotations Validation in Blazor

## Overview

The Register page now implements **localized validation** using:
1. **DataAnnotations** attributes on the model
2. **IValidatableObject** for custom cross-field validation
3. **IStringLocalizer<SharedResource>** for localized error messages
4. **Custom LocalizedDataAnnotationsValidator** component

## How It Works

### 1. Model with DataAnnotations

```csharp
private class RegisterModel : IValidatableObject
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Access IStringLocalizer from the service provider
        var localizer = validationContext.GetService(typeof(IStringLocalizer<SharedResource>)) 
            as IStringLocalizer<SharedResource>;

        // Custom validation with localized messages
        if (Password != ConfirmPassword)
        {
            yield return new ValidationResult(
                localizer?["PasswordsMustMatch"] ?? "Passwords do not match.",
                new[] { nameof(ConfirmPassword) });
        }
    }
}
```

###2. Blazor Form

```razor
<EditForm Model="@_model" OnValidSubmit="HandleSubmitAsync">
    <DataAnnotationsValidator />
    <LocalizedDataAnnotationsValidator />  <!-- Custom component for IValidatableObject -->
    
    <div class="form-group">
        <label>Email</label>
        <input @bind="_model.Email" />
        <ValidationMessage For="@(() => _model.Email)" />
    </div>
    
    <button type="submit">Register</button>
</EditForm>
```

### 3. Custom LocalizedDataAnnotationsValidator

This component handles validation from `IValidatableObject.Validate()`:

```csharp
@inject IServiceProvider ServiceProvider

protected override void OnInitialized()
{
    EditContext.OnValidationRequested += HandleValidationRequested;
}

private void HandleValidationRequested(object? sender, ValidationRequestedEventArgs args)
{
    if (EditContext?.Model is IValidatableObject validatableModel)
    {
        var validationContext = new ValidationContext(EditContext.Model, ServiceProvider, null);
        var validationResults = validatableModel.Validate(validationContext);
        
        // Add validation messages to the store
        foreach (var result in validationResults)
        {
            // ... add to ValidationMessageStore
        }
    }
}
```

## Benefits

✅ **Localized error messages** - Uses SharedResource.resx
✅ **Standard DataAnnotations** - Required, EmailAddress, MinLength, MaxLength
✅ **Custom validation** - Cross-field validation via IValidatableObject
✅ **Real-time feedback** - Validates on field change and form submit
✅ **Clean separation** - Validation logic in the model

## Default DataAnnotations Messages

By default, DataAnnotations uses English error messages. Our custom `IValidatableObject.Validate()` method provides localized messages for custom validation.

For built-in attributes (Required, EmailAddress, etc.), the messages are still in English unless you:
1. Use custom attributes with `ErrorMessage` parameter
2. Create custom validation attributes that use IStringLocalizer
3. Override the default messages globally

## Example: Custom Localized Required Attribute

```csharp
public class LocalizedRequiredAttribute : RequiredAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var result = base.IsValid(value, validationContext);
        
        if (result != ValidationResult.Success)
        {
            var localizer = validationContext.GetService(typeof(IStringLocalizer<SharedResource>)) 
                as IStringLocalizer<SharedResource>;
            
            return new ValidationResult(
                string.Format(localizer?["Required"] ?? "{0} is required.", validationContext.DisplayName));
        }
        
        return result;
    }
}
```

## Current Implementation

The Register page uses:
- **Standard attributes** for basic validation (Required, EmailAddress, MinLength, MaxLength)
- **IValidatableObject** for the "passwords must match" validation with **localized message**
- **ValidationMessage** components to display errors next to each field

## Testing

1. Navigate to `/register`
2. Leave fields empty and click "Create account" → See "The X field is required" (English, from DataAnnotations)
3. Enter different passwords → See "Passwords do not match." (Localized from SharedResource.resx!)
4. Add culture-specific .resx file to test translations

## Future Enhancements

To fully localize all validation messages:
1. Create custom attributes: `LocalizedRequiredAttribute`, `LocalizedEmailAddressAttribute`, etc.
2. Or use FluentValidation.Blazor for full localization support
3. Or override DataAnnotations default messages globally

## Current Status

✅ **Custom validation localized** (e.g., PasswordsMustMatch)
⚠️ **Built-in attributes not localized** (e.g., Required, EmailAddress) - use English defaults
✅ **Infrastructure ready** for full localization via custom attributes or FluentValidation
