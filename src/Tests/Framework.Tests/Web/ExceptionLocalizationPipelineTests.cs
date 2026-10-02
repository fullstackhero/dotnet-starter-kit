using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Web.Exceptions;
using FSH.Framework.Web.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Framework.Tests.Web;

// Pipeline-level counterpart to GlobalExceptionHandlerLocalizationTests. Those tests assign
// CultureInfo.CurrentUICulture by hand and call the handler directly, so they never exercise the one
// thing production depends on: that the culture RequestLocalizationMiddleware negotiates is still
// visible to the handler, which UseExceptionHandler runs from ABOVE that middleware. It is not — the
// assignment lives in the middleware own async frame — so the handler has to read the negotiated
// culture off the request instead.
//
// The ambient culture is pinned to invariant for the duration of each case, which is what a container
// with no LANG gives the API. Without the pin the developer machine culture decides the outcome and a
// pt-BR machine reports a false pass.
public sealed class ExceptionLocalizationPipelineTests
{
    private sealed record Outcome(string? Title, string? Detail, string? Code, string? NegotiatedUiCulture, string? ContentLanguage);

    // The production order: UseExceptionHandler at the top of the pipeline, localization further in.
    private static Task<Outcome> InvokeAsync(string acceptLanguage, Exception exception) =>
        RunAsync(app => app.UseHeroLocalization(), acceptLanguage, exception);

    // A pipeline with no localization at all, standing in for the middleware registered between
    // UseExceptionHandler and UseHeroLocalization: nothing negotiates a culture, so no feature exists.
    private static Task<Outcome> InvokeWithoutLocalizationAsync(string acceptLanguage, Exception exception) =>
        RunAsync(_ => { }, acceptLanguage, exception);

    private static async Task<Outcome> RunAsync(Action<IApplicationBuilder> configure, string acceptLanguage, Exception exception)
    {
        var previousCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        try
        {
            var configuration = new ConfigurationBuilder().Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMetrics();

            // ExceptionHandlerMiddlewareImpl is activated from DI and takes a DiagnosticListener;
            // the real host gets it from WebApplicationBuilder, so a bare ServiceCollection must supply it.
            services.AddSingleton(_ => new DiagnosticListener("Microsoft.AspNetCore"));
            services.AddSingleton<DiagnosticSource>(sp => sp.GetRequiredService<DiagnosticListener>());

            services.AddHeroLocalization(configuration);
            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddProblemDetails();

            await using var provider = services.BuildServiceProvider();

            var app = new ApplicationBuilder(provider);
            app.UseExceptionHandler();
            configure(app);
            app.Run(_ => throw exception);
            var pipeline = app.Build();

            using var scope = provider.CreateScope();
            var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            context.Request.Path = "/api/v1/test";
            context.Request.Headers.AcceptLanguage = acceptLanguage;
            using var body = new MemoryStream();
            context.Response.Body = body;

            await pipeline(context);

            using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(body.ToArray()));
            var root = doc.RootElement;
            var contentLanguage = context.Response.Headers.ContentLanguage.ToString();
            return new Outcome(
                root.TryGetProperty("title", out var t) ? t.GetString() : null,
                root.TryGetProperty("detail", out var d) ? d.GetString() : null,
                root.TryGetProperty("code", out var c) ? c.GetString() : null,
                context.Features.Get<IRequestCultureFeature>()?.RequestCulture.UICulture.Name,
                string.IsNullOrEmpty(contentLanguage) ? null : contentLanguage);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    // Baseline: the middleware DOES negotiate the requested UI culture. If this fails, the gap is in
    // negotiation and the assertions below say nothing about the handler.
    [Theory]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    public async Task Request_culture_is_negotiated_from_the_accept_language_header(string acceptLanguage)
    {
        var exception = new NotFoundException("English fallback") { MessageKey = "Error.NotFound" };

        var outcome = await InvokeAsync(acceptLanguage, exception);

        outcome.NegotiatedUiCulture.ShouldBe(acceptLanguage);
    }

    // The production symptom: a localized exception answered in English despite Accept-Language: pt-BR.
    [Theory]
    [InlineData("pt-BR", "Não encontrado")]
    [InlineData("en-US", "Not Found")]
    public async Task Detail_is_localized_from_the_negotiated_request_culture(string acceptLanguage, string expected)
    {
        var exception = new NotFoundException("English fallback") { MessageKey = "Error.NotFound" };

        var outcome = await InvokeAsync(acceptLanguage, exception);

        outcome.Code.ShouldBe("Error.NotFound");
        outcome.Detail.ShouldBe(expected);
    }

    // Title comes from the status-mapped catalog key through the injected IStringLocalizer, a separate
    // resolution path from Detail IStringLocalizerFactory. Both read the ambient culture, so both broke.
    [Theory]
    [InlineData("pt-BR", "Não encontrado")]
    [InlineData("en-US", "Not Found")]
    public async Task Title_is_localized_from_the_negotiated_request_culture(string acceptLanguage, string expected)
    {
        var outcome = await InvokeAsync(acceptLanguage, new NotFoundException("English fallback"));

        outcome.Title.ShouldBe(expected);
    }

    // A raw exception takes the 500 branch, whose Title and Detail come straight off the injected
    // localizer with no MessageKey involved.
    [Theory]
    [InlineData("pt-BR", "Ocorreu um erro inesperado")]
    [InlineData("en-US", "An unexpected error occurred")]
    public async Task Unexpected_error_is_localized_from_the_negotiated_request_culture(string acceptLanguage, string expected)
    {
        var outcome = await InvokeAsync(acceptLanguage, new InvalidOperationException("boom"));

        outcome.Title.ShouldBe(expected);
    }

    // ExceptionHandlerMiddleware clears the response before re-executing, dropping the Content-Language
    // the localization middleware had written. The handler restores it so the body culture is declared.
    [Theory]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    public async Task Content_language_declares_the_culture_the_body_was_written_in(string acceptLanguage)
    {
        var exception = new NotFoundException("English fallback") { MessageKey = "Error.NotFound" };

        var outcome = await InvokeAsync(acceptLanguage, exception);

        outcome.ContentLanguage.ShouldBe(acceptLanguage);
    }

    // No localization in the pipeline: the handler must still answer, from the ambient culture, and
    // must not claim a Content-Language it never negotiated.
    [Fact]
    public async Task Handler_still_answers_when_no_culture_was_negotiated()
    {
        var exception = new NotFoundException("English fallback") { MessageKey = "Error.NotFound" };

        var outcome = await InvokeWithoutLocalizationAsync("pt-BR", exception);

        outcome.NegotiatedUiCulture.ShouldBeNull();
        outcome.ContentLanguage.ShouldBeNull();
        outcome.Code.ShouldBe("Error.NotFound");
        outcome.Detail.ShouldBe("Not Found");
        outcome.Title.ShouldBe("Not Found");
    }

    // The ambient culture is restored on the way out, so the handler cannot leak the request culture
    // onto whatever else runs on this thread afterwards.
    [Fact]
    public async Task Ambient_culture_is_restored_after_handling()
    {
        var exception = new NotFoundException("English fallback") { MessageKey = "Error.NotFound" };
        var before = CultureInfo.CurrentUICulture;

        await InvokeAsync("pt-BR", exception);

        CultureInfo.CurrentUICulture.ShouldBe(before);
    }
}
