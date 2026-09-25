using FSH.Modules.Notifications.IntegrationEventHandlers;

namespace Notifications.Tests.IntegrationEventHandlers;

/// <summary>
/// Every tenant billing email is built here, and the pair it returns is the whole contract: a
/// <c>Body</c> the provider sends as text/html and a <c>TextBody</c> the provider sends as the
/// text/plain alternative. Passing the markup as both is what this PR fixes downstream, and the only
/// thing that keeps the plain part honest is that it is written separately - so a template that
/// forgot it, or that reused the HTML, has to be caught here.
/// </summary>
public sealed class BillingEmailBodiesTests
{
    private static readonly DateTime ValidUpto = new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    public static TheoryData<string, (string Subject, string Body, string TextBody)> AllTemplates() => new()
    {
        { "NearingExpiry", BillingEmailBodies.NearingExpiry("Acme", "pro", ValidUpto, daysRemaining: 3) },
        { "EnteredGrace", BillingEmailBodies.EnteredGrace("Acme", "pro", ValidUpto, ValidUpto.AddDays(7)) },
        { "Expired", BillingEmailBodies.Expired("Acme", "pro", ValidUpto) },
        { "InvoiceIssued", BillingEmailBodies.InvoiceIssued("INV-2026-0001", 129.50m, "USD", ValidUpto) },
    };

    [Theory]
    [MemberData(nameof(AllTemplates))]
    public void Template_Should_ReturnAnHtmlBodyAndAPlainTextTwin(
        string name,
        (string Subject, string Body, string TextBody) built)
    {
        // Assert
        built.Subject.ShouldNotBeNullOrWhiteSpace(name);

        built.Body.ShouldContain("<p", Case.Sensitive, name);
        built.Body.ShouldContain(built.Subject, Case.Sensitive, name);

        // The plain part is not allowed to be the markup, nor to be empty because someone passed the
        // HTML twice and trimmed it. No angle bracket survives the text twin.
        built.TextBody.ShouldNotBeNullOrWhiteSpace(name);
        built.TextBody.ShouldNotContain("<", Case.Sensitive, name);
        built.TextBody.ShouldContain(built.Subject, Case.Sensitive, name);
        built.TextBody.ShouldContain("This is an automated message.", Case.Sensitive, name);
    }

    [Fact]
    public void NearingExpiry_Should_EscapeTheTenantNameInHtml_ButNotInPlainText()
    {
        // Arrange — a tenant name is operator-supplied and lands inside the markup.
        const string Hostile = "<script>alert(1)</script> & Co";

        // Act
        var (_, body, text) = BillingEmailBodies.NearingExpiry(Hostile, "pro", ValidUpto, daysRemaining: 3);

        // Assert — escaped where it is parsed as markup…
        body.ShouldNotContain("<script>");
        body.ShouldContain("&lt;script&gt;alert(1)&lt;/script&gt; &amp; Co");

        // …and left alone where it is not: entities in a text/plain part are read literally, so
        // escaping there would show the reader "&amp;" instead of "&".
        text.ShouldContain(Hostile);
    }

    [Fact]
    public void InvoiceIssued_Should_DropTheDueLine_When_ThereIsNoDueDate()
    {
        // Act
        var (_, body, text) = BillingEmailBodies.InvoiceIssued("INV-1", 10m, "USD", dueAtUtc: null);

        // Assert — no orphan label, and no blank paragraph where the optional line would have been.
        body.ShouldNotContain("Due by");
        text.ShouldNotContain("Due by");
        text.ShouldNotContain($"{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}");
    }

    [Fact]
    public void NearingExpiry_Should_SayTomorrow_When_OnlyOneDayIsLeft()
    {
        // The singular arm reads as copy, not as a count, so it is the one a template change breaks
        // silently: "expires in 1 days" still renders.
        var (subject, body, text) = BillingEmailBodies.NearingExpiry("Acme", "pro", ValidUpto, daysRemaining: 1);

        subject.ShouldBe("Your subscription expires tomorrow");
        body.ShouldContain(subject);
        text.ShouldContain(subject);
    }
}
