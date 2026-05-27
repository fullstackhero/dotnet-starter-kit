using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Mailing;
using FSH.Framework.Mailing.Services;
using FSH.Modules.Identity.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Events;

/// <summary>
/// Sends a welcome email when a new user registers.
/// </summary>
public sealed class UserRegisteredEmailHandler
    : IIntegrationEventHandler<UserRegisteredIntegrationEvent>
{
    private readonly IMailService _mailService;
    private readonly ILogger<UserRegisteredEmailHandler> _logger;

    public UserRegisteredEmailHandler(
        IMailService mailService,
        ILogger<UserRegisteredEmailHandler> logger)
    {
        _mailService = mailService;
        _logger = logger;
    }

    public async Task HandleAsync(UserRegisteredIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (string.IsNullOrWhiteSpace(@event.Email))
        {
            return;
        }

        try
        {
            var mail = new MailRequest(
                to: new System.Collections.ObjectModel.Collection<string> { @event.Email },
                subject: "Welcome!",
                body: $"Hi {@event.FirstName}, thanks for registering.");

            await _mailService.SendAsync(mail, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Email failures must not break user registration.
            // The email can be retried via the outbox/dead-letter mechanism.
            _logger.LogWarning(ex, "Failed to send welcome email to {Email}", @event.Email);
        }
    }
}