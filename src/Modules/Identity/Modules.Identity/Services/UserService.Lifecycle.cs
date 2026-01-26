using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Constants;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Identity.Domain;
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

        var context = await BuildToggleContextAsync(userId, activateUser, cancellationToken);
        
        await ValidateTogglePermissionsAsync(context, cancellationToken);
        
        ApplyStatusChange(context);
        
        await SaveAndAuditAsync(context, cancellationToken);
    }

    private async Task<ToggleStatusContext> BuildToggleContextAsync(
        string userId, 
        bool activateUser, 
        CancellationToken cancellationToken)
    {
        var actorId = _currentUser.GetUserId();
        if (actorId == Guid.Empty)
        {
            throw new UnauthorizedException("authenticated user required to toggle status");
        }

        var actor = await userManager.FindByIdAsync(actorId.ToString())
            ?? throw new UnauthorizedException("current user not found");

        var targetUser = await userManager.Users
            .Where(u => u.Id == userId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("User Not Found.");

        return new ToggleStatusContext(
            ActorId: actorId,
            Actor: actor,
            TargetUser: targetUser,
            ActivateUser: activateUser,
            TenantId: multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id);
    }

    private async Task ValidateTogglePermissionsAsync(
        ToggleStatusContext context, 
        CancellationToken cancellationToken)
    {
        if (!await userManager.IsInRoleAsync(context.Actor, RoleConstants.Admin))
        {
            await AuditPolicyFailureAsync(context, "ActorNotAdmin", cancellationToken);
            throw new CustomException("Only administrators can toggle user status.");
        }

        if (!context.ActivateUser && context.ActorId.ToString() == context.TargetUser.Id)
        {
            await AuditPolicyFailureAsync(context, "SelfDeactivationBlocked", cancellationToken);
            throw new CustomException("Users cannot deactivate themselves.");
        }

        if (await userManager.IsInRoleAsync(context.TargetUser, RoleConstants.Admin))
        {
            await AuditPolicyFailureAsync(context, "AdminDeactivationBlocked", cancellationToken);
            throw new CustomException("Administrators cannot be deactivated.");
        }

        if (!context.ActivateUser)
        {
            await EnsureMinimumActiveAdminsAsync(context, cancellationToken);
        }
    }

    private async Task EnsureMinimumActiveAdminsAsync(
        ToggleStatusContext context, 
        CancellationToken cancellationToken)
    {
        var activeAdmins = await userManager.GetUsersInRoleAsync(RoleConstants.Admin);
        if (!activeAdmins.Any(u => u.IsActive))
        {
            await AuditPolicyFailureAsync(context, "NoActiveAdmins", cancellationToken);
            throw new CustomException("Tenant must have at least one active administrator.");
        }
    }

    private static void ApplyStatusChange(ToggleStatusContext context)
    {
        if (context.ActivateUser)
        {
            context.TargetUser.Activate(context.ActorId.ToString(), context.TenantId);
        }
        else
        {
            context.TargetUser.Deactivate(context.ActorId.ToString(), "Status toggled by administrator", context.TenantId);
        }
    }

    private async Task SaveAndAuditAsync(
        ToggleStatusContext context, 
        CancellationToken cancellationToken)
    {
        var result = await userManager.UpdateAsync(context.TargetUser);
        if (!result.Succeeded)
        {
            throw new CustomException("Toggle status failed", result.Errors.Select(e => e.Description).ToList());
        }

        await _auditClient.WriteActivityAsync(
            ActivityKind.Command,
            name: "ToggleUserStatus",
            statusCode: 204,
            durationMs: 0,
            captured: BodyCapture.None,
            requestSize: 0,
            responseSize: 0,
            requestPreview: new { actorId = context.ActorId.ToString(), targetUserId = context.TargetUser.Id, action = context.ActivateUser ? "activate" : "deactivate", tenant = context.TenantId ?? "unknown" },
            responsePreview: new { outcome = "success" },
            severity: AuditSeverity.Information,
            source: "Identity",
            ct: cancellationToken).ConfigureAwait(false);
    }

    private async Task AuditPolicyFailureAsync(
        ToggleStatusContext context, 
        string reason, 
        CancellationToken cancellationToken)
    {
        var claims = new Dictionary<string, object?>
        {
            ["actorId"] = context.ActorId.ToString(),
            ["targetUserId"] = context.TargetUser.Id,
            ["tenant"] = context.TenantId ?? "unknown",
            ["action"] = context.ActivateUser ? "activate" : "deactivate"
        };

        await _auditClient.WriteSecurityAsync(
            SecurityAction.PolicyFailed,
            subjectId: context.ActorId.ToString(),
            reasonCode: reason,
            claims: claims,
            severity: AuditSeverity.Warning,
            source: "Identity",
            ct: cancellationToken).ConfigureAwait(false);
    }

    private sealed record ToggleStatusContext(
        Guid ActorId,
        FshUser Actor,
        FshUser TargetUser,
        bool ActivateUser,
        string? TenantId);
}
