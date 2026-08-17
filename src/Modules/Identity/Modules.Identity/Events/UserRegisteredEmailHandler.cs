using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Mailing;
using FSH.Framework.Mailing.Services;
using FSH.Modules.Identity.Contracts.Events;
using FSH.Modules.Identity.Services;
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
            // The body is sent as text/html, so the name — user-supplied — has to be encoded or a
            // first name containing '<' is parsed as markup instead of shown.
            var greeting = $"Hi {@event.FirstName}, thanks for registering.";
            var mail = new MailRequest(
                to: new System.Collections.ObjectModel.Collection<string> { @event.Email },
                subject: "Welcome!",
                body: HtmlEmail.Notice("Welcome!", greeting),
                textBody: greeting);

            await _mailService.SendAsync(mail, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Email failures must not break user registration.
            // The email can be retried via the outbox/dead-letter mechanism.
            // PII minimization: identify the recipient by UserId, not email address.
            _logger.LogWarning(ex, "Failed to send welcome email to user {UserId}", @event.UserId);
        }
    }
}