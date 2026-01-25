using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Caching;
using FSH.Framework.Core.Context;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Jobs.Services;
using FSH.Framework.Mailing.Services;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Storage.Services;
using FSH.Framework.Web.Origin;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Identity.Services;

internal sealed partial class UserService(
    UserManager<FshUser> userManager,
    SignInManager<FshUser> signInManager,
    RoleManager<FshRole> roleManager,
    IdentityDbContext db,
    ICacheService cache,
    IJobService jobService,
    IMailService mailService,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    IStorageService storageService,
    IOutboxStore outboxStore,
    IOptions<OriginOptions> originOptions,
    IHttpContextAccessor httpContextAccessor,
    ICurrentUser currentUser,
    IAuditClient auditClient,
    IPasswordHistoryService passwordHistoryService,
    IPasswordExpiryService passwordExpiryService
    ) : IUserService
{
    private readonly Uri? _originUrl = originOptions.Value.OriginUrl;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly IAuditClient _auditClient = auditClient;
    private readonly IPasswordHistoryService _passwordHistoryService = passwordHistoryService;
    private readonly IPasswordExpiryService _passwordExpiryService = passwordExpiryService;

    private void EnsureValidTenant()
    {
        if (string.IsNullOrWhiteSpace(multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id))
        {
            throw new FSH.Framework.Core.Exceptions.UnauthorizedException("invalid tenant");
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

        // For relative paths from local storage, prefix with the API origin and wwwroot.
        if (_originUrl is null)
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request is not null && !string.IsNullOrWhiteSpace(request.Scheme) && request.Host.HasValue)
            {
                var baseUri = $"{request.Scheme}://{request.Host.Value}{request.PathBase}";
                var relativePath = imageUrl.ToString().TrimStart('/');
                return $"{baseUri.TrimEnd('/')}/{relativePath}";
            }

            return imageUrl.ToString();
        }

        var originRelativePath = imageUrl.ToString().TrimStart('/');
        return $"{_originUrl.AbsoluteUri.TrimEnd('/')}/{originRelativePath}";
    }
}
