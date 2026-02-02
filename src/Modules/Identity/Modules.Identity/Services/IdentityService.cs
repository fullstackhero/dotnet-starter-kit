using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FSH.Modules.Identity.Services;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<FshUser> _userManager;
    private readonly ILogger<IdentityService> _logger;
    private readonly IMultiTenantContextAccessor<AppTenantInfo>? _multiTenantContextAccessor;
    private readonly IGroupRoleService _groupRoleService;

    public IdentityService(
        UserManager<FshUser> userManager,
        IMultiTenantContextAccessor<AppTenantInfo>? multiTenantContextAccessor,
        ILogger<IdentityService> logger,
        IGroupRoleService groupRoleService)
    {
        _userManager = userManager;
        _multiTenantContextAccessor = multiTenantContextAccessor;
        _logger = logger;
        _groupRoleService = groupRoleService;
    }

    public async Task<(string Subject, IEnumerable<Claim> Claims)?>
        ValidateCredentialsAsync(string email, string password, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(password);

        var tenant = GetValidatedTenant();
        var user = await FindAndValidateUserByCredentialsAsync(email, password);

        ValidateUserStatus(user);
        ValidateTenantStatus(tenant);

        var claims = await BuildUserClaimsAsync(user, tenant.Id, ct);
        return (user.Id, claims);
    }

    public async Task<(string Subject, IEnumerable<Claim> Claims)?>
        ValidateRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var tenant = GetValidatedTenant();
        var user = await FindUserByRefreshTokenAsync(refreshToken, tenant.Id, ct);

        ValidateRefreshTokenExpiry(user);
        ValidateUserStatus(user);
        ValidateTenantStatus(tenant);

        var claims = await BuildUserClaimsAsync(user, tenant.Id, ct);
        return (user.Id, claims);
    }

    public async Task StoreRefreshTokenAsync(string subject, string refreshToken, DateTime expiresAtUtc, CancellationToken ct = default)
    {
        var tenant = GetValidatedTenant();
        var user = await _userManager.FindByIdAsync(subject)
            ?? throw new UnauthorizedException("user not found");

        var hashedToken = HashToken(refreshToken);
        user.RefreshToken = hashedToken;
        user.RefreshTokenExpiryTime = expiresAtUtc;

        _logger.LogDebug(
            "Storing refresh token for user {UserId} in tenant {TenantId}. Token hash: {TokenHash}, Expires: {ExpiresAt}",
            subject, tenant.Id, hashedToken[..Math.Min(8, hashedToken.Length)] + "...", expiresAtUtc);

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            _logger.LogError("Failed to persist refresh token for user {UserId}: {Errors}",
                subject, string.Join(", ", result.Errors.Select(e => e.Description)));
            throw new UnauthorizedException("could not persist refresh token");
        }
    }

    private AppTenantInfo GetValidatedTenant()
    {
        var tenant = _multiTenantContextAccessor!.MultiTenantContext.TenantInfo
            ?? throw new UnauthorizedException();

        if (string.IsNullOrWhiteSpace(tenant.Id))
        {
            throw new UnauthorizedException();
        }

        return tenant;
    }

    private async Task<FshUser> FindAndValidateUserByCredentialsAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email.Trim().Normalize());
        if (user is null || !await _userManager.CheckPasswordAsync(user, password))
        {
            throw new UnauthorizedException();
        }

        return user;
    }

    private async Task<FshUser> FindUserByRefreshTokenAsync(string refreshToken, string tenantId, CancellationToken ct)
    {
        var hashedToken = HashToken(refreshToken);

        _logger.LogDebug(
            "Validating refresh token for tenant {TenantId}. Token hash: {TokenHash}",
            tenantId, hashedToken[..Math.Min(8, hashedToken.Length)] + "...");

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == hashedToken, ct);

        if (user is null)
        {
            _logger.LogWarning("No user found with matching refresh token hash for tenant {TenantId}", tenantId);
            throw new UnauthorizedException("refresh token is invalid or expired");
        }

        return user;
    }

    private void ValidateRefreshTokenExpiry(FshUser user)
    {
        if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            _logger.LogWarning(
                "Refresh token expired for user {UserId}. Expired at: {ExpiryTime}, Current time: {CurrentTime}",
                user.Id, user.RefreshTokenExpiryTime, DateTime.UtcNow);
            throw new UnauthorizedException("refresh token is invalid or expired");
        }
    }

    private static void ValidateUserStatus(FshUser user)
    {
        if (!user.IsActive)
        {
            throw new UnauthorizedException("user is deactivated");
        }

        if (!user.EmailConfirmed)
        {
            throw new UnauthorizedException("email not confirmed");
        }
    }

    private static void ValidateTenantStatus(AppTenantInfo tenant)
    {
        if (tenant.Id == MultitenancyConstants.Root.Id)
        {
            return;
        }

        if (!tenant.IsActive)
        {
            throw new UnauthorizedException($"tenant {tenant.Id} is deactivated");
        }

        if (DateTime.UtcNow > tenant.ValidUpto)
        {
            throw new UnauthorizedException($"tenant {tenant.Id} validity has expired");
        }
    }

    private async Task<List<Claim>> BuildUserClaimsAsync(FshUser user, string tenantId, CancellationToken ct)
    {
        var claims = CreateBasicClaims(user, tenantId);
        await AddRoleClaimsAsync(claims, user, ct);
        return claims;
    }

    private static List<Claim> CreateBasicClaims(FshUser user, string tenantId) =>
    [
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new(ClaimTypes.NameIdentifier, user.Id),
        new(ClaimTypes.Email, user.Email!),
        new(ClaimTypes.Name, user.FirstName ?? string.Empty),
        new(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty),
        new(ClaimConstants.Fullname, $"{user.FirstName} {user.LastName}"),
        new(ClaimTypes.Surname, user.LastName ?? string.Empty),
        new(ClaimConstants.Tenant, tenantId),
        new(ClaimConstants.ImageUrl, user.ImageUrl?.ToString() ?? string.Empty)
    ];

    private async Task AddRoleClaimsAsync(List<Claim> claims, FshUser user, CancellationToken ct)
    {
        var directRoles = await _userManager.GetRolesAsync(user);
        var groupRoles = await _groupRoleService.GetUserGroupRolesAsync(user.Id, ct);

        var allRoles = directRoles.Union(groupRoles).Distinct();
        claims.AddRange(allRoles.Select(r => new Claim(ClaimTypes.Role, r)));
    }

    private static string HashToken(string token)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
