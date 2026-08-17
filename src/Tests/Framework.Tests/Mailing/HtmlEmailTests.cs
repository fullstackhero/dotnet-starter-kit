using FSH.Framework.Mailing;

namespace Framework.Tests.Mailing;

public sealed class HtmlEmailTests
{
    #region Encoding

    [Fact]
    public void Encode_Should_NeutraliseMarkup_When_ValueContainsTags()
    {
        // Act
        var encoded = HtmlEmail.Encode("<script>alert(1)</script>");

        // Assert
        encoded.ShouldNotContain("<script>");
        encoded.ShouldBe("&lt;script&gt;alert(1)&lt;/script&gt;");
    }

    [Fact]
    public void Encode_Should_EncodeQuotes_When_ValueContainsThem()
    {
        // The hand-rolled escaper this replaced covered only & < >, which is safe in element
        // content but not in an attribute. Quotes are the difference that made it the weaker one.

        // Act
        var encoded = HtmlEmail.Encode("a \" and an ' quote");

        // Assert
        encoded.ShouldNotContain("\"");
        encoded.ShouldNotContain("'");
        encoded.ShouldBe("a &quot; and an &#39; quote");
    }

    [Fact]
    public void Encode_Should_EncodeOnce_When_ValueIsAlreadyEscaped()
    {
        // Guards against a second pass turning &amp; into &amp;amp; and showing the entity to the user.

        // Act
        var encoded = HtmlEmail.Encode("Tom &amp; Jerry");

        // Assert
        encoded.ShouldBe("Tom &amp;amp; Jerry");
        HtmlEmail.Encode("Tom & Jerry").ShouldBe("Tom &amp; Jerry");
    }

    [Fact]
    public void Encode_Should_EntitiseLatin1ButNotHigherPlanes_When_ValueIsNonAscii()
    {
        // WebUtility.HtmlEncode is asymmetric, and the asymmetry is worth pinning rather than
        // discovering: characters in the Latin-1 supplement (160-255) become numeric entities,
        // while anything above stays verbatim and relies on the declared utf-8. Both forms render
        // the same. Pinned because entitising accents IS a change for billing mail, whose previous
        // hand-rolled escaper left them raw; identity mail already behaved this way.

        // Act & Assert
        HtmlEmail.Encode("Conceição").ShouldBe("Concei&#231;&#227;o");
        HtmlEmail.Encode("日本語").ShouldBe("日本語");
    }

    [Fact]
    public void Encode_Should_ReturnEmpty_When_ValueIsEmpty()
    {
        HtmlEmail.Encode(string.Empty).ShouldBeEmpty();
    }

    [Fact]
    public void Encode_Should_Throw_When_ValueIsNull()
    {
        Should.Throw<ArgumentNullException>(() => HtmlEmail.Encode(null!));
    }

    #endregion

    #region Shell

    [Fact]
    public void Shell_Should_EmitCompleteDocument_When_Called()
    {
        // Act
        var html = HtmlEmail.Shell("Heading", "<p>body</p>");

        // Assert
        html.ShouldStartWith("<!DOCTYPE html>");
        html.ShouldContain("<meta charset=\"utf-8\">");
        html.ShouldContain("<title>Heading</title>");
        html.ShouldContain("<p>body</p>");
    }

    [Fact]
    public void Shell_Should_EncodeHeading_When_HeadingContainsMarkup()
    {
        // Act
        var html = HtmlEmail.Shell("<b>Hi</b>", "<p>body</p>");

        // Assert — the heading is plain text on both the title and the h1.
        html.ShouldNotContain("<b>Hi</b>");
        html.ShouldContain("&lt;b&gt;Hi&lt;/b&gt;");
    }

    [Fact]
    public void Shell_Should_InsertInnerHtmlVerbatim_When_ItContainsMarkup()
    {
        // Pins the documented contract: innerHtml is trusted markup built by the caller and is NOT
        // encoded. If someone "hardens" this by encoding it, every e-mail renders as visible tags.

        // Act
        var html = HtmlEmail.Shell("Heading", "<p><strong>bold</strong></p>");

        // Assert
        html.ShouldContain("<p><strong>bold</strong></p>");
        html.ShouldNotContain("&lt;strong&gt;");
    }

    [Fact]
    public void Shell_Should_Throw_When_ArgumentIsNull()
    {
        Should.Throw<ArgumentNullException>(() => HtmlEmail.Shell(null!, "<p>x</p>"));
        Should.Throw<ArgumentNullException>(() => HtmlEmail.Shell("Heading", null!));
    }

    #endregion

    #region LinkAction

    [Fact]
    public void LinkAction_Should_RenderAnchor_When_Called()
    {
        // A bare URL inside a text/html part is not auto-linked by most clients, which is the bug
        // this shell exists to prevent.

        // Act
        var html = HtmlEmail.LinkAction("Reset your password", "Use the link below.", "https://app.test/reset", "Reset password");

        // Assert
        html.ShouldContain("<a href=\"https://app.test/reset\"");
        html.ShouldContain(">Reset password</a>");
    }

    [Fact]
    public void LinkAction_Should_EncodeQuerySeparator_When_UrlHasMultipleParameters()
    {
        // Entity-encoding & inside an href is correct per spec. The verbatim URL belongs in the
        // text/plain alternative, which is where the link-shape assertions live.

        // Act
        var html = HtmlEmail.LinkAction("Confirm", "Intro", "https://app.test/c?token=a&email=b%2Bc", "Confirm");

        // Assert
        html.ShouldContain("token=a&amp;email=b%2Bc");
        html.ShouldNotContain("token=a&email=b%2Bc");
    }

    [Fact]
    public void LinkAction_Should_KeepUrlInsideAttribute_When_UrlContainsAQuote()
    {
        // An unencoded quote would close the href attribute and let the rest of the URL become markup.

        // Act
        var html = HtmlEmail.LinkAction("Heading", "Intro", "https://app.test/x?q=\"onmouseover=alert(1)", "Go");

        // Assert — the raw quote must not appear inside the attribute value, which is what would
        // close href early and turn the rest of the URL into attributes. Asserting on the encoded
        // form alone is not enough: it would still pass if the raw form were emitted as well.
        html.ShouldNotContain("q=\"onmouseover");
        html.ShouldContain("&quot;onmouseover=alert(1)");
    }

    [Fact]
    public void LinkAction_Should_EncodeIntroAndLabel_When_TheyContainMarkup()
    {
        // Act
        var html = HtmlEmail.LinkAction("Heading", "<i>intro</i>", "https://app.test/x", "<i>label</i>");

        // Assert
        html.ShouldNotContain("<i>intro</i>");
        html.ShouldNotContain("<i>label</i>");
        html.ShouldContain("&lt;i&gt;intro&lt;/i&gt;");
        html.ShouldContain("&lt;i&gt;label&lt;/i&gt;");
    }

    #endregion

    #region Notice

    [Fact]
    public void Notice_Should_EncodeMessage_When_MessageContainsMarkup()
    {
        // Act
        var html = HtmlEmail.Notice("Welcome!", "Hi <script>alert(1)</script>, thanks for registering.");

        // Assert
        html.ShouldStartWith("<!DOCTYPE html>");
        html.ShouldNotContain("<script>");
        html.ShouldContain("&lt;script&gt;alert(1)&lt;/script&gt;");
    }

    #endregion
}
