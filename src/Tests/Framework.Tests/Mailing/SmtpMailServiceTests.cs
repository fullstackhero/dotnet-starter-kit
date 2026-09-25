using FSH.Framework.Mailing;
using FSH.Framework.Mailing.Services;
using MimeKit;

namespace Framework.Tests.Mailing;

// SMTP is the default provider (UseSendGrid defaults to false), and the body mapping this PR changed
// had a gate only on the optional one. The transport needs a server and is not what changed; the MIME
// shape is, so that is what these drive, through the real builder.
public sealed class SmtpMailServiceTests
{
    private static async Task<MimeMessage> BuildAsync(MailRequest request)
    {
        var email = new MimeMessage();
        await SmtpMailService.AddAttachmentsAsync(email, request, CancellationToken.None);
        return email;
    }

    [Fact]
    public async Task Body_Should_CarryBothParts_When_TheCallerSuppliesText()
    {
        // Arrange
        var request = new MailRequest(
            to: ["dest@x.com"],
            subject: "hi",
            body: "<p>rich</p>",
            textBody: "plain");

        // Act
        using var email = await BuildAsync(request);

        // Assert — a text-only client reads the plain part; sending the markup as both is what this
        // fixes, and it is exactly what a client that cannot render HTML would then display.
        email.HtmlBody.ShouldBe("<p>rich</p>");
        email.TextBody.ShouldBe("plain");
        email.Body.ShouldNotBeNull().ContentType.MimeType.ShouldBe("multipart/alternative");
    }

    [Fact]
    public async Task Body_Should_BeHtmlOnly_When_TheCallerSuppliesNoText()
    {
        // Every caller outside the templated notifications still passes Body alone. Inventing a plain
        // part from the markup is what the old code effectively did, so assert it does not.
        var request = new MailRequest(to: ["dest@x.com"], subject: "hi", body: "<p>rich</p>");

        using var email = await BuildAsync(request);

        email.HtmlBody.ShouldBe("<p>rich</p>");
        email.TextBody.ShouldBeNull();
        email.Body.ShouldNotBeNull().ContentType.MimeType.ShouldBe("text/html");
    }

    [Fact]
    public async Task Body_Should_KeepBothParts_When_AnAttachmentIsPresent()
    {
        // The attachment wraps the alternative in a multipart/mixed. A builder that appended the
        // attachment to the wrong part would lose the plain text and still "have" an attachment.
        var request = new MailRequest(
            to: ["dest@x.com"],
            subject: "hi",
            body: "<p>rich</p>",
            textBody: "plain",
            attachmentData: new Dictionary<string, byte[]> { ["invoice.pdf"] = [1, 2, 3] });

        using var email = await BuildAsync(request);

        email.Body.ShouldNotBeNull().ContentType.MimeType.ShouldBe("multipart/mixed");
        email.HtmlBody.ShouldBe("<p>rich</p>");
        email.TextBody.ShouldBe("plain");
        email.Attachments.Single().ContentDisposition.ShouldNotBeNull().FileName.ShouldBe("invoice.pdf");
    }
}
