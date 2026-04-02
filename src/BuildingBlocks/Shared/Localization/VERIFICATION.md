# Blazor Localization Verification Guide

## Current Setup Summary

✅ **SharedResource.resx** configured in `BuildingBlocks\Shared\Localization\`
✅ **SharedResource.cs** marker class created
✅ **LocalizationExtensions** updated with `ResourcesPath = "Localization"`
✅ **Blazor Program.cs** already calls `AddFshLocalization()` and `UseFshLocalization()`
✅ **Blazor project** references `Shared` project

## How to Verify Localization is Working

### 1. Test with the Register Page

The Register page (`/register`) now uses `IStringLocalizer<SharedResource>` for validation messages.

**Steps to test:**
1. Navigate to `/register` in the Blazor app
2. Fill in the registration form
3. Enter different passwords in "Password" and "Confirm Password"
4. Click "Create account"
5. You should see the localized error message: "Passwords do not match."

This message comes from `Localizer["PasswordsMustMatch"]` which reads from `SharedResource.resx`.

### 2. Test with Different Cultures

Create a culture-specific resource file to test localization:

1. Create `SharedResource.af-ZA.resx` in the same folder
2. Add the key `PasswordsMustMatch` with value `Wagwoorde stem nie ooreen nie.`
3. Set culture cookie in browser console:
```javascript
document.cookie = ".AspNetCore.Culture=c=af-ZA|uic=af-ZA; path=/";
```
4. Refresh and test the Register page again
5. You should see the Afrikaans error message

### 3. Inject IStringLocalizer in Any Component

```razor
@page "/your-page"
@inject IStringLocalizer<SharedResource> Localizer

<MudText>@Localizer["Required", "Email"]</MudText>
<MudText>@Localizer["InvalidEmail"]</MudText>
```

### 4. Use in Validators

When creating validators, inject `IStringLocalizer<SharedResource>`:

```csharp
public class MyValidator : AbstractValidator<MyCommand>
{
    public MyValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage(IdentityValidationMessages.Required("Email", localizer))
            .EmailAddress()
            .WithMessage(IdentityValidationMessages.InvalidEmail(localizer));
    }
}
```

## Testing Different Cultures

### Option 1: Using Browser Developer Tools

1. Open browser DevTools (F12)
2. Go to Console
3. Set a culture cookie:
```javascript
document.cookie = ".AspNetCore.Culture=c=af-ZA|uic=af-ZA; path=/";
```
4. Refresh the page

### Option 2: Using Query String (if supported)

Navigate to: `?culture=af-ZA&ui-culture=af-ZA`

### Option 3: Create Culture-Specific Resource Files

1. In Visual Studio, right-click `SharedResource.resx`
2. Select "Add" > "New Item"
3. Choose "Resources File"
4. Name it: `SharedResource.af-ZA.resx` (for Afrikaans)
5. Add the same keys with translated values

Example for `SharedResource.af-ZA.resx`:
- `Required` → `{0} is vereis.`
- `InvalidEmail` → `'n Geldige e-posadres is vereis.`

## Common Issues and Solutions

### Issue: Localizer returns "[Required]" instead of the message

**Cause**: The key doesn't exist in the .resx file or the namespace/path is incorrect.

**Solution**:
1. Verify the key exists in `SharedResource.resx`
2. Check that `SharedResource.cs` namespace is `FSH.Framework.Shared.Localization`
3. Ensure the .resx file is set as `EmbeddedResource` (default in Visual Studio)

### Issue: "Cannot inject IStringLocalizer"

**Cause**: Localization services not registered.

**Solution**: Verify `Program.cs` has:
```csharp
builder.Services.AddFshLocalization();
```

### Issue: Culture not changing

**Cause**: Request localization middleware not configured.

**Solution**: Verify `Program.cs` has (after authentication):
```csharp
app.UseFshLocalization();
```

## Architecture Overview

```
BuildingBlocks/Shared/Localization/
├── SharedResource.resx           ← Default (English) messages
├── SharedResource.af-ZA.resx     ← Afrikaans translations (optional)
├── SharedResource.cs             ← Marker class
├── LocalizationExtensions.cs     ← Service registration
└── LocalizationConstants.cs      ← Supported cultures

Modules/Identity/Constants/
└── IdentityValidationMessages.cs ← Helper methods that use SharedResource

Playground/Playground.Blazor/
├── Program.cs                     ← Calls AddFshLocalization() & UseFshLocalization()
└── Components/Pages/Authentication/
    └── Register.razor             ← Real-world example using IStringLocalizer
```

## Next Steps

1. **Test the Register page** at `/register` with mismatched passwords
2. **Verify the localized message** is displayed correctly
3. **Add culture-specific .resx files** for your target languages (e.g., `SharedResource.es.resx` for Spanish)
4. **Update validators** to inject `IStringLocalizer<SharedResource>` for runtime localization
5. **Test with different cultures** using browser cookies

## Real-World Example

The **Register page** (`Playground\Playground.Blazor\Components\Pages\Authentication\Register.razor`) demonstrates:

```csharp
@inject IStringLocalizer<SharedResource> Localizer

// In code:
if (_model.Password != _model.ConfirmPassword)
{
    _errorMessage = Localizer["PasswordsMustMatch"];
    return;
}
```

This shows how to use localized messages in real Blazor components for:
- ✅ Client-side validation
- ✅ Error messages
- ✅ User-facing text that needs translation

## Important Notes

- The `.resx` file must be in the `Localization` folder (matches `ResourcesPath`)
- The marker class namespace must match the folder structure: `FSH.Framework.Shared.Localization`
- For backward compatibility, `IdentityValidationMessages` methods have optional `localizer` parameter
- Without passing `localizer`, English defaults are used
- With `localizer`, runtime culture-specific messages are returned
- The Register page (`/register`) is a working example of localization in action
