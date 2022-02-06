using FSH.WebApi.Domain.Common.Contracts;

namespace FSH.WebApi.Infrastructure.Identity.Events;

internal class ApplicationRoleUpdatedEvent : DomainEvent
{
    public string RoleId { get; set; } = default!;
    public string RoleName { get; set; } = default!;
    public bool PermissionsUpdated { get; set; }

    public ApplicationRoleUpdatedEvent(string roleId, string roleName, bool permissionsUpdated = false) =>
        (RoleId, RoleName, PermissionsUpdated) = (roleId, roleName, permissionsUpdated);
}