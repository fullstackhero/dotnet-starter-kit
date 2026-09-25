using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Storage;
using FSH.Framework.Storage;
using FSH.Framework.Storage.Services;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Domain;
using FSH.Modules.Identity.Localization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace FSH.Modules.Identity.Services;

internal sealed class UserProfileService(
    UserManager<FshUser> userManager,
    SignInManager<FshUser> signInManager,
    IStorageService storageService,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    IRequestContextService requestContext,
    IdentityErrorDescriber errorDescriber) : IUserProfileService
{
    public async Task<UserDto> GetAsync(string userId, CancellationToken cancellationToken)
    {
        // Relies on Finbuckle's tenant filter — callers can only ever read
        // their own user record, which is in the request's resolved tenant.
        var user = await userManager.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new NotFoundException("user not found")
        {
            MessageKey = "Identity.UserNotFound",
            ResourceSource = typeof(IdentityResources),
        };

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ImageUrl = ResolveImageUrl(user.ImageUrl),
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            TwoFactorEnabled = user.TwoFactorEnabled,
            Locale = user.Locale,
            ConcurrencyStamp = user.ConcurrencyStamp,
        };
    }

    public Task<int> GetCountAsync(CancellationToken cancellationToken) =>
        userManager.Users.AsNoTracking().CountAsync(cancellationToken);

    public async Task<List<UserDto>> GetListAsync(CancellationToken cancellationToken)
    {
        var users = await userManager.Users.AsNoTracking().ToListAsync(cancellationToken);
        var result = new List<UserDto>(users.Count);
        foreach (var user in users)
        {
            result.Add(new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ImageUrl = ResolveImageUrl(user.ImageUrl),
                IsActive = user.IsActive
            });
        }

        return result;
    }

    public async Task UpdateAsync(string userId, string firstName, string lastName, string phoneNumber, FileUploadRequest image, bool deleteCurrentImage, string? locale, IReadOnlyList<string>? expectedConcurrencyStamps, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);

        _ = user ?? throw new NotFoundException("user not found")
        {
            MessageKey = "Identity.UserNotFound",
            ResourceSource = typeof(IdentityResources),
        };

        // This is a full-representation update, so a caller working from a stale read would
        // silently blank whatever changed since. The precondition is checked here, before the
        // storage calls below: a rejected update must not leave an orphan upload behind, and on
        // the deleteCurrentImage path it must not remove the avatar with no database change.
        EnsureConcurrencyStampMatches(user, expectedConcurrencyStamps);

        // The old blob is only deleted once the database write has gone through. UpdateAsync can
        // still lose a race here — the If-Match check above is not the last word, because another
        // writer can land between it and the save — and deleting first would leave AspNetUsers
        // pointing at a blob that no longer exists, which no retry can repair.
        string? replacedBlob = null;
        Uri imageUri = user.ImageUrl ?? null!;
        // image is optional: text-only edits forward a null FileUploadRequest, so guard before
        // dereferencing Data or the common no-image update path NREs.
        if (image?.Data != null)
        {
            var imageString = await storageService.UploadAsync<FshUser>(image, FileType.Image, cancellationToken);
            user.ImageUrl = new Uri(imageString, UriKind.RelativeOrAbsolute);
            if (deleteCurrentImage && imageUri != null)
            {
                replacedBlob = imageUri.ToString();
            }
        }
        else if (deleteCurrentImage && imageUri != null)
        {
            replacedBlob = imageUri.ToString();
            user.ImageUrl = null;
        }

        user.FirstName = firstName;
        user.LastName = lastName;
        // An absent locale means "not provided by this update" — preserve the existing value so a
        // text-only profile edit never clears a language the user already chose. Blank counts as
        // absent, matching the validator: its allow-list rule is guarded by
        // .When(!IsNullOrWhiteSpace), so "" never reaches the allow-list and must not reach the
        // user either. A form that serialises its untouched locale field as "" would otherwise
        // wipe the preference on every unrelated save.
        if (!string.IsNullOrWhiteSpace(locale))
        {
            user.Locale = locale;
        }

        string? currentPhoneNumber = await userManager.GetPhoneNumberAsync(user);
        if (phoneNumber != currentPhoneNumber)
        {
            await userManager.SetPhoneNumberAsync(user, phoneNumber);
        }

        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            // Identity's store answers a lost race with ConcurrencyFailure instead of throwing,
            // so it would otherwise surface as a generic 500. It is the same condition the
            // If-Match check above reports, just detected one layer down: another writer landed
            // between our read and our save.
            if (result.Errors.Any(error => string.Equals(error.Code, errorDescriber.ConcurrencyFailure().Code, StringComparison.Ordinal)))
            {
                throw StaleProfileException();
            }

            throw new CustomException("Update profile failed")
            {
                MessageKey = "Identity.UpdateProfileFailed",
                ResourceSource = typeof(IdentityResources),
            };
        }

        if (replacedBlob is not null)
        {
            await storageService.RemoveAsync(replacedBlob, cancellationToken);
        }

        await signInManager.RefreshSignInAsync(user);
    }

    private static void EnsureConcurrencyStampMatches(FshUser user, IReadOnlyList<string>? expectedConcurrencyStamps)
    {
        // A null list means the caller sent no If-Match and accepts the stored version as-is.
        // ponytail: keep the precondition optional for backward compatibility; a future major can
        // require it and answer 428 Precondition Required when the header is missing.
        if (expectedConcurrencyStamps is null)
        {
            return;
        }

        var storedStamp = user.ConcurrencyStamp;
        if (storedStamp is null || !expectedConcurrencyStamps.Contains(storedStamp, StringComparer.Ordinal))
        {
            throw StaleProfileException();
        }
    }

    private static CustomException StaleProfileException() =>
        new(
            "The profile changed since you loaded it. Reload it and apply your changes again.",
            errors: null,
            HttpStatusCode.PreconditionFailed);

    public async Task SetImageUrlAsync(string userId, string? imageUrl, CancellationToken cancellationToken)
    {
        EnsureValidTenant();
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("user not found")
            {
                MessageKey = "Identity.UserNotFound",
                ResourceSource = typeof(IdentityResources),
            };

        user.ImageUrl = string.IsNullOrWhiteSpace(imageUrl)
            ? null
            : new Uri(imageUrl, UriKind.RelativeOrAbsolute);

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new CustomException("Update profile image failed")
            {
                MessageKey = "Identity.UpdateProfileImageFailed",
                ResourceSource = typeof(IdentityResources),
            };
        }

        await signInManager.RefreshSignInAsync(user);
    }

    public async Task<bool> ExistsWithEmailAsync(string email, string? exceptId = null, CancellationToken cancellationToken = default)
    {
        EnsureValidTenant();
        return await userManager.FindByEmailAsync(email.Normalize()) is FshUser user && user.Id != exceptId;
    }

    public async Task<bool> ExistsWithNameAsync(string name, CancellationToken cancellationToken = default)
    {
        EnsureValidTenant();
        return await userManager.FindByNameAsync(name) is not null;
    }

    public async Task<bool> ExistsWithPhoneNumberAsync(string phoneNumber, string? exceptId = null, CancellationToken cancellationToken = default)
    {
        EnsureValidTenant();
        return await userManager.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber, cancellationToken) is FshUser user && user.Id != exceptId;
    }

    private void EnsureValidTenant()
    {
        if (string.IsNullOrWhiteSpace(multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id))
        {
            throw new UnauthorizedException("invalid tenant")
            {
                MessageKey = "Error.InvalidTenant",
            };
        }
    }

    private string? ResolveImageUrl(Uri? imageUrl)
    {
        if (imageUrl is null)
        {
            return null;
        }

        // Absolute URLs (e.g., S3) pass through unchanged.
        if (imageUrl.IsAbsoluteUri)
        {
            return imageUrl.ToString();
        }

        // For relative paths from local storage, prefix with the API origin (configured, else the request host).
        var baseUri = requestContext.Origin;
        if (string.IsNullOrEmpty(baseUri))
        {
            return imageUrl.ToString();
        }

        var relativePath = imageUrl.ToString().TrimStart('/');
        return $"{baseUri}/{relativePath}";
    }
}