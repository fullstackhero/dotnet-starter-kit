using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Constants;
using FSH.Modules.Auditing.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Identity.Services;

internal sealed partial class UserService
{
    public async Task DeleteAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);

        _ = user ?? throw new NotFoundException("User Not Found.");

        user.IsActive = false;
        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(error => error.Description).ToList();
            throw new CustomException("Delete profile failed", errors);
        }
    }

    public async Task ToggleStatusAsync(bool activateUser, string userId, CancellationToken cancellationToken)
    {
        EnsureValidTenant();

        var actorId = _currentUser.GetUserId();
        if (actorId == Guid.Empty)
        {
            throw new UnauthorizedException("authenticated user required to toggle status");
        }

        var actor = await userManager.FindByIdAsync(actorId.ToString());
        _ = actor ?? throw new UnauthorizedException("current user not found");

        async ValueTask AuditPolicyFailureAsync(string reason, CancellationToken ct)
        {
            var tenant = multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id ?? "unknown";
            var claims = new Dictionary<string, object?>
            {
                ["actorId"] = actorId.ToString(),
                ["targetUserId"] = userId,
                ["tenant"] = tenant,
                ["action"] = activateUser ? "activate" : "deactivate"
            };

            await _auditClient.WriteSecurityAsync(
                SecurityAction.PolicyFailed,
                subjectId: actorId.ToString(),
                reasonCode: reason,
                claims: claims,
                severity: AuditSeverity.Warning,
                source: "Identity",
                ct: ct).ConfigureAwait(false);
        }

        if (!await userManager.IsInRoleAsync(actor, RoleConstants.Admin))
        {
            await AuditPolicyFailureAsync("ActorNotAdmin", cancellationToken);
            throw new CustomException("Only administrators can toggle user status.");
        }

        if (!activateUser && string.Equals(actor.Id, userId, StringComparison.Ordinal))
        {
            await AuditPolicyFailureAsync("SelfDeactivationBlocked", cancellationToken);
            throw new CustomException("Users cannot deactivate themselves.");
        }

        var user = await userManager.Users.Where(u => u.Id == userId).FirstOrDefaultAsync(cancellationToken);
        _ = user ?? throw new NotFoundException("User Not Found.");

        bool targetIsAdmin = await userManager.IsInRoleAsync(user, RoleConstants.Admin);
        if (targetIsAdmin)
        {
            await AuditPolicyFailureAsync("AdminDeactivationBlocked", cancellationToken);
            throw new CustomException("Administrators cannot be deactivated.");
        }

        if (!activateUser)
        {
            var activeAdmins = await userManager.GetUsersInRoleAsync(RoleConstants.Admin);
            int activeAdminCount = activeAdmins.Count(u => u.IsActive);
            if (activeAdminCount == 0)
            {
                await AuditPolicyFailureAsync("NoActiveAdmins", cancellationToken);
                throw new CustomException("Tenant must have at least one active administrator.");
            }
        }

        var tenantId = multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id;
        if (activateUser)
        {
            user.Activate(actorId.ToString(), tenantId);
        }
        else
        {
            user.Deactivate(actorId.ToString(), "Status toggled by administrator", tenantId);
        }

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(error => error.Description).ToList();
            throw new CustomException("Toggle status failed", errors);
        }

        await _auditClient.WriteActivityAsync(
            ActivityKind.Command,
            name: "ToggleUserStatus",
            statusCode: 204,
            durationMs: 0,
            captured: BodyCapture.None,
            requestSize: 0,
            responseSize: 0,
            requestPreview: new { actorId = actorId.ToString(), targetUserId = userId, action = activateUser ? "activate" : "deactivate", tenant = tenantId ?? "unknown" },
            responsePreview: new { outcome = "success" },
            severity: AuditSeverity.Information,
            source: "Identity",
            ct: cancellationToken).ConfigureAwait(false);
    }
}
