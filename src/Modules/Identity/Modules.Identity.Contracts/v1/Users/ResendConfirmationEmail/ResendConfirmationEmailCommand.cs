using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Users.ResendConfirmationEmail;

/// <summary>
/// Re-sends the email-confirmation link to an unconfirmed user. Gated by
/// <c>Permissions.Users.ConfirmEmail</c> at the endpoint. <see cref="Origin"/> is the request base
/// URL used to build the confirmation link (set by the endpoint).
/// </summary>
public sealed record ResendConfirmationEmailCommand(string UserId, string Origin) : ICommand<Unit>;
