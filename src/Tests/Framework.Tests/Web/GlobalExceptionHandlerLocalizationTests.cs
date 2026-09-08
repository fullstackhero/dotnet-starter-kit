using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Web.Exceptions;
using Framework.Tests.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Framework.Tests.Web;

// Handler-level (Docker-free) proof that GlobalExceptionHandler localizes the ProblemDetails body
// from the shared resx under the ambient UI culture. Covers both the framework branches (raw
// KeyNotFoundException/InvalidOperationException) and the CustomException branch, whose Title now
// comes from the status-mapped catalog key and whose Detail is resolved from MessageKey (falling
// back to the English Message when no key is set).
public sealed class GlobalExceptionHandlerLocalizationTests
{
    private static async Task<(string? Title, string? Detail)> HandleAsync(Exception exception, string culture)
    {
        var (title, detail, _) = await HandleWithCodeAsync(exception, culture);
        return (title, detail);
    }

    private static async Task<(string? Title, string? Detail, string? Code)> HandleWithCodeAsync(Exception exception, string culture)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(culture);

            var context = new DefaultHttpContext();
            context.Request.Path = "/api/v1/test";
            using var body = new MemoryStream();
            context.Response.Body = body;

            var handler = new GlobalExceptionHandler(
                NullLogger<GlobalExceptionHandler>.Instance,
                SharedResourcesLocalizerFactory.Create(),
                SharedResourcesLocalizerFactory.CreateFactory());
            await handler.TryHandleAsync(context, exception, CancellationToken.None);

            var json = Encoding.UTF8.GetString(body.ToArray());
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var title = root.TryGetProperty("title", out var t) ? t.GetString() : null;
            var detail = root.TryGetProperty("detail", out var d) ? d.GetString() : null;
            var code = root.TryGetProperty("code", out var c) ? c.GetString() : null;
            return (title, detail, code);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Theory]
    [InlineData("pt-BR", "Não encontrado")]
    [InlineData("en-US", "Not Found")]
    public async Task NotFound_title_is_localized(string culture, string expected)
    {
        var (title, _) = await HandleAsync(new KeyNotFoundException("missing"), culture);
        title.ShouldBe(expected);
    }

    [Theory]
    [InlineData("pt-BR", "Ocorreu um erro inesperado")]
    [InlineData("en-US", "An unexpected error occurred")]
    public async Task Unexpected_title_is_localized(string culture, string expected)
    {
        var (title, _) = await HandleAsync(new InvalidOperationException("boom"), culture);
        title.ShouldBe(expected);
    }

    // CustomException Title is now the status-mapped catalog key (404 -> Error.NotFound), localized.
    [Theory]
    [InlineData("pt-BR", "Não encontrado")]
    [InlineData("en-US", "Not Found")]
    public async Task CustomException_title_is_localized_by_status(string culture, string expected)
    {
        var (title, _) = await HandleAsync(new NotFoundException("some entity was not found"), culture);
        title.ShouldBe(expected);
    }

    // Detail resolves from MessageKey under the request culture (using an existing Core key).
    [Theory]
    [InlineData("pt-BR", "Não autorizado")]
    [InlineData("en-US", "Unauthorized")]
    public async Task CustomException_detail_is_localized_from_key(string culture, string expected)
    {
        var exception = new UnauthorizedException("english fallback") { MessageKey = "Error.Unauthorized" };
        var (_, detail) = await HandleAsync(exception, culture);
        detail.ShouldBe(expected);
    }

    // No MessageKey: Detail falls back to the literal (English) Message regardless of culture (non-breaking).
    [Theory]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    public async Task CustomException_detail_falls_back_to_message_without_key(string culture)
    {
        var (_, detail) = await HandleAsync(new NotFoundException("Plain English detail."), culture);
        detail.ShouldBe("Plain English detail.");
    }

    // Parameterless UnauthorizedException carries Error.AuthenticationFailed, so generic auth failures
    // localize their Detail without any call-site key (English fallback stays "Authentication failed.").
    [Theory]
    [InlineData("pt-BR", "Falha na autenticação.")]
    [InlineData("en-US", "Authentication failed.")]
    public async Task Parameterless_unauthorized_detail_is_localized(string culture, string expected)
    {
        var (_, detail) = await HandleAsync(new UnauthorizedException(), culture);
        detail.ShouldBe(expected);
    }

    // Unknown MessageKey: ResourceNotFound path falls back to the English Message, never leaks the raw key.
    [Fact]
    public async Task CustomException_detail_falls_back_when_key_missing()
    {
        var exception = new NotFoundException("English fallback detail.") { MessageKey = "Does.Not.Exist" };
        var (_, detail) = await HandleAsync(exception, "pt-BR");
        detail.ShouldBe("English fallback detail.");
    }

    // BCL-subclass exceptions (kept as their base type so audit severity classification is unaffected)
    // still localize their Detail from MessageKey through the shared handler path.
    [Theory]
    [InlineData("pt-BR", "Falha na autenticação.")]
    [InlineData("en-US", "Authentication failed.")]
    public async Task LocalizedUnauthorizedAccess_detail_is_localized(string culture, string expected)
    {
        var exception = new LocalizedUnauthorizedAccessException("english fallback")
        {
            MessageKey = "Error.AuthenticationFailed",
        };
        var (_, detail) = await HandleAsync(exception, culture);
        detail.ShouldBe(expected);
    }

    [Theory]
    [InlineData("pt-BR", "Não encontrado")]
    [InlineData("en-US", "Not Found")]
    public async Task LocalizedKeyNotFound_detail_is_localized(string culture, string expected)
    {
        var exception = new LocalizedKeyNotFoundException("english fallback")
        {
            MessageKey = "Error.NotFound",
        };
        var (_, detail) = await HandleAsync(exception, culture);
        detail.ShouldBe(expected);
    }

    // No MessageKey on a localized subclass → Detail falls back to the literal (English) message.
    [Fact]
    public async Task LocalizedUnauthorizedAccess_without_key_falls_back_to_message()
    {
        var (_, detail) = await HandleAsync(new LocalizedUnauthorizedAccessException("Plain English."), "pt-BR");
        detail.ShouldBe("Plain English.");
    }

    // Detail is prose under the request culture, so the MessageKey travels as a stable "code" extension:
    // clients branch on the code instead of matching localized text. Same key in every culture.
    [Theory]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    public async Task CustomException_surfaces_the_message_key_as_code(string culture)
    {
        var exception = new UnauthorizedException("english fallback") { MessageKey = "Error.Unauthorized" };
        var (_, _, code) = await HandleWithCodeAsync(exception, culture);
        code.ShouldBe("Error.Unauthorized");
    }

    // A localized BCL subclass carries its key through the same path.
    [Fact]
    public async Task LocalizedKeyNotFound_surfaces_the_message_key_as_code()
    {
        var exception = new LocalizedKeyNotFoundException("english fallback") { MessageKey = "Error.NotFound" };
        var (_, _, code) = await HandleWithCodeAsync(exception, "pt-BR");
        code.ShouldBe("Error.NotFound");
    }

    // No key → no code property at all, rather than a null or an invented one.
    [Fact]
    public async Task Exception_without_key_omits_the_code()
    {
        var (_, _, code) = await HandleWithCodeAsync(new NotFoundException("Plain English detail."), "pt-BR");
        code.ShouldBeNull();
    }

    // An unknown key still travels as the code even though Detail fell back to English: the code is the
    // contract, the resx lookup is presentation.
    [Fact]
    public async Task Unknown_key_still_surfaces_as_code()
    {
        var exception = new NotFoundException("English fallback detail.") { MessageKey = "Does.Not.Exist" };
        var (_, detail, code) = await HandleWithCodeAsync(exception, "pt-BR");
        detail.ShouldBe("English fallback detail.");
        code.ShouldBe("Does.Not.Exist");
    }

    // A raw BCL exception (no ILocalizableMessage) keeps its message and gets no code.
    [Fact]
    public async Task Raw_key_not_found_has_no_code()
    {
        var (_, detail, code) = await HandleWithCodeAsync(new KeyNotFoundException("missing"), "pt-BR");
        detail.ShouldBe("missing");
        code.ShouldBeNull();
    }

    // A 409 must not be titled "an unexpected error occurred". Before Error.Conflict existed the
    // status-to-key map fell through to Error.Unexpected — which RESOLVES, so the type-name fallback
    // never fired and every conflict in the API reported a title contradicting its own status and its
    // own detail. There are 41 Conflict throw sites across Billing and Catalog.
    [Theory]
    [InlineData("en-US", "Conflict")]
    [InlineData("pt-BR", "Conflito")]
    public async Task Conflict_is_titled_as_a_conflict_not_as_unexpected(string culture, string expected)
    {
        var exception = new CustomException("Brand name already taken.", [], HttpStatusCode.Conflict);

        var (title, _) = await HandleAsync(exception, culture);

        title.ShouldBe(expected);
    }

    // A status with no title of its own keeps the pre-localization behaviour — the exception type
    // name — instead of claiming the error was unexpected. Status-consistent beats confidently wrong.
    [Theory]
    [InlineData("en-US")]
    [InlineData("pt-BR")]
    public async Task Untranslated_status_falls_back_to_the_exception_type_name(string culture)
    {
        var exception = new CustomException("Mailbox is locked.", [], HttpStatusCode.Locked);

        var (title, _) = await HandleAsync(exception, culture);

        title.ShouldBe(nameof(CustomException));
    }
}
