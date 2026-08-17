using System.Net;
using FSH.Framework.Mailing;
using FSH.Framework.Mailing.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Framework.Tests.Mailing;

// REL-01: SendGridMailService must not swallow a non-2xx SendGrid reply (the client is built with
// HttpErrorAsException=false, so a failure comes back as a Response, not an exception). Transient
// failures (429/5xx) throw so the caller's Hangfire job retries; permanent failures (other 4xx) are
// logged and returned — retrying a bad key / rejected recipient only floods the dead-letter queue.
public sealed class SendGridMailServiceTests
{
    private static SendGridMailService BuildService(ISendGridClient client)
    {
        var options = Options.Create(new MailOptions
        {
            UseSendGrid = true,
            From = "noreply@x.com",
            SendGrid = new SendGridOptions { ApiKey = "sg-key", From = "noreply@x.com" },
        });
        return new SendGridMailService(options, client, NullLogger<SendGridMailService>.Instance);
    }

    private static ISendGridClient ClientReturning(HttpStatusCode status)
    {
        var client = Substitute.For<ISendGridClient>();
        client.SendEmailAsync(Arg.Any<SendGridMessage>(), Arg.Any<CancellationToken>())
            .Returns(new Response(status, null, null));
        return client;
    }

    private static MailRequest ValidRequest() =>
        new(to: ["dest@x.com"], subject: "hi", body: "body");

    [Fact]
    public async Task SendAsync_Should_MapTheBodies_ToTheirOwnMimeParts()
    {
        // Arrange — Body and TextBody are distinct parts. Sending Body as both shipped raw markup to
        // text-only clients.
        var client = ClientReturning(HttpStatusCode.Accepted);
        var service = BuildService(client);
        var request = new MailRequest(
            to: ["dest@x.com"],
            subject: "hi",
            body: "<p>rich</p>",
            textBody: "plain");

        // Act
        await service.SendAsync(request, CancellationToken.None);

        // Assert
        // CreateSingleEmail folds both bodies into Contents (the HtmlContent/PlainTextContent properties
        // stay null once the message is built), so assert on the MIME parts themselves.
        var sent = (SendGridMessage)client.ReceivedCalls().Single().GetArguments()[0]!;
        sent.Contents.Single(c => c.Type == "text/html").Value.ShouldBe("<p>rich</p>");
        sent.Contents.Single(c => c.Type == "text/plain").Value.ShouldBe("plain");
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]        // 429 — rate limited
    [InlineData(HttpStatusCode.InternalServerError)]    // 500 — SendGrid-side
    [InlineData(HttpStatusCode.ServiceUnavailable)]     // 503 — SendGrid-side
    public async Task SendAsync_When_TransientFailure_Should_Throw_ForRetry(HttpStatusCode status)
    {
        var service = BuildService(ClientReturning(status));

        var send = async () => await service.SendAsync(ValidRequest(), CancellationToken.None);

        // Throwing routes the send back through the caller's Hangfire automatic retry.
        await send.ShouldThrowAsync<Exception>();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]   // 401 — bad API key
    [InlineData(HttpStatusCode.BadRequest)]     // 400 — rejected recipient / malformed
    [InlineData(HttpStatusCode.Forbidden)]      // 403 — sender not verified
    public async Task SendAsync_When_PermanentRejection_Should_NotThrow_ToAvoidRetryStorm(HttpStatusCode status)
    {
        var service = BuildService(ClientReturning(status));

        var send = async () => await service.SendAsync(ValidRequest(), CancellationToken.None);

        // Logged as an error (surfaced to ops) but not thrown — a retry cannot make it succeed.
        await send.ShouldNotThrowAsync();
    }

    [Fact]
    public async Task SendAsync_When_SendGridReturnsAccepted_Should_Complete()
    {
        var service = BuildService(ClientReturning(HttpStatusCode.Accepted));

        var send = async () => await service.SendAsync(ValidRequest(), CancellationToken.None);

        await send.ShouldNotThrowAsync();
    }
}
