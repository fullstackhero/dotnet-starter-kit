using FSH.WebApi.Domain.Common.Contracts;

namespace FSH.WebApi.Infrastructure.Identity.Events;

internal class ApplicationUserUpdatedEvent : DomainEvent
{
    public string UserId { get; set; } = default!;
    public bool RolesUpdated { get; set; }

    public ApplicationUserUpdatedEvent(string userId, bool rolesUpdated = false) =>
        (UserId, RolesUpdated) = (userId, rolesUpdated);
}