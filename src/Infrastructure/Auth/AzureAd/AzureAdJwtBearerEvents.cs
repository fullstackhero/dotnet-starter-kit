using Finbuckle.MultiTenant;
using FSH.WebApi.Application.Common.Exceptions;
using FSH.WebApi.Application.Identity.Users;
using FSH.WebApi.Shared.Authorization;
using FSH.WebApi.Shared.Multitenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace FSH.WebApi.Infrastructure.Auth.AzureAd;

internal class AzureAdJwtBearerEvents : JwtBearerEvents
{
    private readonly ILogger _logger;
    private readonly IConfiguration _config;

    public AzureAdJwtBearerEvents(ILogger logger, IConfiguration config) =>
        (_logger, _config) = (logger, config);

    public override Task AuthenticationFailed(AuthenticationFailedContext context)
    {
        _logger.AuthenticationFailed(context.Exception);
        return base.AuthenticationFailed(context);
    }

    public override Task MessageReceived(MessageReceivedContext context)
    {
        _logger.TokenReceived();
        return base.MessageReceived(context);
    }

    /// <summary>
    /// This method contains the logic that validates the user's tenant and normalizes claims.
    /// </summary>
    /// <param name="context">The validated token context.</param>
    /// <returns>A task.</returns>
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var principal = context.Principal;

        (string issuer, string objectId, string tenantId) =
            await GetValuesFromPrincipal(principal, context.HttpContext.RequestServices);

        // the caller comes from an admin-consented, recorded issuer
        var identity = principal.Identities.First();

        // Adding tenant claim
        identity.AddClaim(new Claim(FSHClaims.Tenant, tenantId));

        // Creating a new scope so the new tenant id gets picked up via the tenant claim
        // Todo: check if this is still necessary or can be replaced with
        // _httpContextAccessor.HttpContext.TrySetTenantInfo(tenant, true)
        using var scope = context.HttpContext.RequestServices.CreateScope();

        // Lookup local user or create one if none exist.
        string userId = await scope.ServiceProvider.GetRequiredService<IIdentityService>()
            .GetOrCreateFromPrincipalAsync(principal);

        // we use the nameidentifier claim to store the user id
        var idClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
        identity.TryRemoveClaim(idClaim);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));

        // and the email claim for the email
        var upnClaim = principal.FindFirst(ClaimTypes.Upn);
        if (upnClaim is not null)
        {
            var emailClaim = principal.FindFirst(ClaimTypes.Email);
            identity.TryRemoveClaim(emailClaim);
            identity.AddClaim(new Claim(ClaimTypes.Email, upnClaim.Value));
        }

        _logger.TokenValidationSucceeded(objectId, issuer);
    }

    private async Task<(string Issuer, string ObjectId, string TenantId)> GetValuesFromPrincipal(
        [NotNull] ClaimsPrincipal? principal, IServiceProvider serviceProvider)
    {
        string? issuer = principal?.GetIssuer();
        string? objectId = principal?.GetObjectId();
        _logger.TokenValidationStarted(objectId, issuer);

        if (principal is null || issuer is null || objectId is null)
        {
            _logger.TokenValidationFailed(objectId, issuer);
            throw new UnauthorizedException("Authentication Failed.");
        }

        // shortcut when the issuer is the rootIssuer defined in appsettings
        if (issuer == _config["SecuritySettings:AzureAd:RootIssuer"])
        {
            return (issuer, objectId, MultitenancyConstants.Root.Id);
        }

        // Todo: check if this is still necessary
        // creating a scope otherwise the current scope gets polluted with an ApplicationDbContext without a tenant set
        // using var scope = serviceProvider.CreateScope();

        if (await serviceProvider.GetRequiredService<IMultiTenantStore<TenantInfo>>()
                .TryGetByIdentifierAsync(issuer)
            is not TenantInfo tenant)
        {
            _logger.TokenValidationFailed(objectId, issuer);

            // the caller was not from a trusted issuer - throw to block the authentication flow
            throw new UnauthorizedException("Authentication Failed.");
        }

        return (issuer, objectId, tenant.Id);
    }
}

internal static class AzureAdJwtBearerEventsLoggingExtensions
{
    public static void AuthenticationFailed(this ILogger logger, Exception e) =>
        logger.Error("Authentication failed Exception: {e}", e);

    public static void TokenReceived(this ILogger logger) =>
        logger.Debug("Received a bearer token");

    public static void TokenValidationStarted(this ILogger logger, string? userId, string? issuer) =>
        logger.Debug("Token Validation Started for User: {userId} Issuer: {issuer}", userId, issuer);

    public static void TokenValidationFailed(this ILogger logger, string? userId, string? issuer) =>
        logger.Warning("Tenant is not registered User: {userId} Issuer: {issuer}", userId, issuer);

    public static void TokenValidationSucceeded(this ILogger logger, string userId, string issuer) =>
        logger.Debug("Token validation succeeded: User: {userId} Issuer: {issuer}", userId, issuer);
}