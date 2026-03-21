using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Messages;
using FSH.Modules.Identity.Contracts.Events;

namespace FSH.Modules.Identity.Events;

/// <summary>
/// Sends a welcome email when a new user registers.
/// </summary>
public sealed class UserRegisteredEmailHandler
    : IIntegrationEventHandler<UserRegisteredIntegrationEvent>
{
    private readonly IMailService _mailService;

    public UserRegisteredEmailHandler(IMailService mailService)
    {
        _mailService = mailService;
    }

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

        await _mailService.SendAsync(mail, ct).ConfigureAwait(false);
    }
}
