using FSH.Framework.Mailing;
using FSH.Framework.Mailing.Services;
using FSH.Modules.Identity.Contracts.Events;
using FSH.Modules.Identity.Events;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Identity.Tests.Events;

/// <summary>
/// The welcome mail is sent as text/html, so a user-supplied first name reaches an HTML parser. It has to
/// be encoded, and the message needs a text/plain twin like every other mail the kit sends.
/// </summary>
public sealed class UserRegisteredEmailHandlerTests
{
    private readonly IMailService _mailService = Substitute.For<IMailService>();

    private UserRegisteredEmailHandler CreateSut() =>
        new(_mailService, NullLogger<UserRegisteredEmailHandler>.Instance);

    private static UserRegisteredIntegrationEvent EventWithFirstName(string firstName) =>
        new(
            Id: Guid.NewGuid(),
            OccurredOnUtc: DateTime.UtcNow,
            TenantId: "root",
            CorrelationId: Guid.NewGuid().ToString(),
            Source: "self-registration",
            UserId: Guid.NewGuid().ToString(),
            Email: "new.user@codefi.com.br",
            FirstName: firstName,
            LastName: "Maciel");

    private MailRequest CaptureSentMail()
    {
        var call = _mailService.ReceivedCalls().Single();
        return (MailRequest)call.GetArguments()[0]!;
    }

    [Fact]
    public async Task HandleAsync_Should_EncodeTheFirstName_When_ItContainsMarkup()
    {
        // Arrange — a first name is user-supplied; unencoded, this closes the surrounding element and
        // injects a tag into the rendered mail.
        var sut = CreateSut();

        // Act
        await sut.HandleAsync(EventWithFirstName("<script>alert(1)</script>"), CancellationToken.None);

        // Assert
        var body = CaptureSentMail().Body!;
        body.ShouldNotContain("<script>");
        body.ShouldContain("&lt;script&gt;");
    }

    [Fact]
    public async Task HandleAsync_Should_SendATextAlternative()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.HandleAsync(EventWithFirstName("Marcelo"), CancellationToken.None);

        // Assert
        var mail = CaptureSentMail();
        mail.TextBody.ShouldBe("Hi Marcelo, thanks for registering.");
        mail.Body!.ShouldContain("Hi Marcelo, thanks for registering.");
    }

    [Fact]
    public async Task HandleAsync_Should_NotSend_When_TheEventCarriesNoEmail()
    {
        // Arrange — nothing to send to; the handler must not build a message at all.
        var sut = CreateSut();
        var @event = EventWithFirstName("Marcelo") with { Email = string.Empty };

        // Act
        await sut.HandleAsync(@event, CancellationToken.None);

        // Assert
        await _mailService.DidNotReceive().SendAsync(Arg.Any<MailRequest>(), Arg.Any<CancellationToken>());
    }
}
