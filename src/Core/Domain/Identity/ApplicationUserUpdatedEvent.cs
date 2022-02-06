namespace FSH.WebApi.Domain.Identity;

public class ApplicationUserUpdatedEvent : DomainEvent
{
    public string UserId { get; set; } = default!;
    public bool RolesUpdated { get; set; }

    public ApplicationUserUpdatedEvent(string userId, bool rolesUpdated = false) =>
        (UserId, RolesUpdated) = (userId, rolesUpdated);
}