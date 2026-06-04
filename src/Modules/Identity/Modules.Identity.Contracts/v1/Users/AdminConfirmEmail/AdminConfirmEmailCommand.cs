using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Users.AdminConfirmEmail;

/// <summary>
/// Administratively confirms a user's email (no confirmation token). Gated by
/// <c>Permissions.Users.ConfirmEmail</c> at the endpoint.
/// </summary>
public sealed record AdminConfirmEmailCommand(string UserId) : ICommand<Unit>;
