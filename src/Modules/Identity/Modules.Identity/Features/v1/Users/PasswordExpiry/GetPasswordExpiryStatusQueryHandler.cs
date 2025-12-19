using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.PasswordExpiry;
using FSH.Framework.Shared.Identity.Claims;
using Mediator;
using Microsoft.AspNetCore.Http;

namespace FSH.Modules.Identity.Features.v1.Users.PasswordExpiry;

public sealed class GetPasswordExpiryStatusQueryHandler : IQueryHandler<GetPasswordExpiryStatusQuery, PasswordExpiryStatusDto>
{
    private readonly IPasswordExpiryService _passwordExpiryService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetPasswordExpiryStatusQueryHandler(
        IPasswordExpiryService passwordExpiryService,
        IHttpContextAccessor httpContextAccessor)
    {
        _passwordExpiryService = passwordExpiryService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async ValueTask<PasswordExpiryStatusDto> Handle(GetPasswordExpiryStatusQuery query, CancellationToken cancellationToken)
    {
        var userId = _httpContextAccessor.HttpContext?.User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            throw new InvalidOperationException("User is not authenticated.");
        }

        var isExpired = await _passwordExpiryService.IsPasswordExpiredAsync(userId, cancellationToken);
        var daysUntilExpiry = await _passwordExpiryService.GetDaysUntilPasswordExpiryAsync(userId, cancellationToken);
        var shouldWarn = await _passwordExpiryService.ShouldWarnAboutPasswordExpiryAsync(userId, cancellationToken);

        return new PasswordExpiryStatusDto
        {
            IsExpired = isExpired,
            DaysUntilExpiry = daysUntilExpiry,
            ShouldWarn = shouldWarn,
            PasswordExpiryDays = _passwordExpiryService.GetPasswordExpiryDays(),
            WarningDaysBeforeExpiry = _passwordExpiryService.GetWarningDaysBeforeExpiry()
        };
    }
}
