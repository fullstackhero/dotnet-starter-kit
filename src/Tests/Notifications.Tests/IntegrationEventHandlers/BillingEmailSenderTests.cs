using System.Collections.ObjectModel;
using FSH.Framework.Mailing;
using FSH.Framework.Mailing.Services;
using FSH.Modules.Notifications.IntegrationEventHandlers;
using Microsoft.Extensions.Logging;

namespace Notifications.Tests.IntegrationEventHandlers;

/// <summary>
/// The send every billing integration-event handler goes through. It is best-effort by design: the
/// handler runs inside the transaction of whatever created/renewed/scanned the tenant, so a dead SMTP
/// host must not take that down with it.
/// </summary>
public sealed class BillingEmailSenderTests
{
    private sealed class RecordingMailService : IMailService
    {
        public List<MailRequest> Sent { get; } = [];

        public Task SendAsync(MailRequest request, CancellationToken ct)
        {
            Sent.Add(request);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingMailService : IMailService
    {
        public Task SendAsync(MailRequest request, CancellationToken ct) =>
            throw new InvalidOperationException("smtp is down");
    }

    /// <summary>Captures what actually reached the sink, which is the only way to assert what did not.</summary>
    private sealed class CapturingLogger : ILogger
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            Messages.Add(formatter(state, exception));
        }
    }

    [Fact]
    public async Task SendAsync_Should_CarryBothBodiesThrough()
    {
        // Arrange
        var mail = new RecordingMailService();

        // Act
        await BillingEmailSender.SendAsync(
            mail,
            new CapturingLogger(),
            email: "tenant@example.com",
            subject: "s",
            body: "<p>rich</p>",
            textBody: "plain",
            context: "nearing-expiry",
            CancellationToken.None);

        // Assert — the plain part has to survive the hop from the template to the provider; dropping
        // it here would leave the provider sending the markup as both parts again.
        var request = mail.Sent.ShouldHaveSingleItem();
        request.To.ShouldBe(new Collection<string> { "tenant@example.com" });
        request.Subject.ShouldBe("s");
        request.Body.ShouldBe("<p>rich</p>");
        request.TextBody.ShouldBe("plain");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendAsync_Should_SendNothing_When_TheTenantHasNoAddress(string? email)
    {
        var mail = new RecordingMailService();

        await BillingEmailSender.SendAsync(
            mail, new CapturingLogger(), email, "s", "<p>b</p>", "b", "nearing-expiry", CancellationToken.None);

        mail.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task SendAsync_Should_SwallowTheFailure_When_TheProviderThrows()
    {
        // Arrange — the caller is an integration-event handler inside someone else's transaction.
        var logger = new CapturingLogger();

        // Act
        await Should.NotThrowAsync(() => BillingEmailSender.SendAsync(
            new ThrowingMailService(),
            logger,
            email: "tenant@example.com",
            subject: "s",
            body: "<p>b</p>",
            textBody: "b",
            context: "nearing-expiry",
            CancellationToken.None));

        // Assert — swallowed, but not silently: the operation is identifiable in the log.
        logger.Messages.ShouldHaveSingleItem().ShouldContain("nearing-expiry");
    }

    [Fact]
    public async Task SendAsync_Should_KeepTheRecipientOutOfTheLog_When_TheProviderThrows()
    {
        // The log is an external sink and an address is PII (CodeQL cs/exposure-of-sensitive-information).
        // The context string is what identifies the failure; the recipient is not needed to act on it.
        var logger = new CapturingLogger();

        await BillingEmailSender.SendAsync(
            new ThrowingMailService(),
            logger,
            email: "tenant@example.com",
            subject: "s",
            body: "<p>b</p>",
            textBody: "b",
            context: "nearing-expiry",
            CancellationToken.None);

        logger.Messages.ShouldHaveSingleItem().ShouldNotContain("tenant@example.com");
    }
}
