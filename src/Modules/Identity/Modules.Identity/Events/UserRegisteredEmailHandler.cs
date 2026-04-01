using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Mailing;
using FSH.Framework.Mailing.Services;
using FSH.Modules.Identity.Contracts.Events;

namespace FSH.Modules.Identity.Events;

/// <summary>
/// Sends a welcome email when a new user registers.
/// </summary>
public sealed class UserRegisteredEmailHandler(IMailService mailService)
        : IIntegrationEventHandler<UserRegisteredIntegrationEvent>
{
    public async Task HandleAsync(UserRegisteredIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (string.IsNullOrWhiteSpace(@event.Email))
        {
            return;
        }

        var mail = new MailRequest(
            to: new System.Collections.ObjectModel.Collection<string> { @event.Email },
            subject: "Welcome!",
            body: $"Hi {@event.FirstName}, thanks for registering.");

        await mailService.SendAsync(mail, ct).ConfigureAwait(false);
    }
}