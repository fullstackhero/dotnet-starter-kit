using FSH.Framework.Web.Idempotency;
using FSH.Framework.Web.RateLimiting;
using FSH.Framework.Web.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Framework.Tests.Web;

public sealed class OptionsDefaultsTests
{
    #region SecurityHeadersOptions

    [Fact]
    public void SecurityHeadersOptions_Should_HaveSecureDefaults_When_Constructed()
    {
        // Act
        var options = new SecurityHeadersOptions();

        // Assert
        options.Enabled.ShouldBeTrue();
        options.AllowInlineStyles.ShouldBeTrue();
        options.ExcludedPaths.ShouldBe(["/scalar", "/openapi"]);
        options.ScriptSources.ShouldBeEmpty();
        options.StyleSources.ShouldBeEmpty();
    }

    #endregion

    #region RateLimitingOptions

    [Fact]
    public void RateLimitingOptions_Should_HaveDefaultPolicies_When_Constructed()
    {
        // Act
        var options = new RateLimitingOptions();

        // Assert
        options.Enabled.ShouldBeTrue();
        options.Tenant.PermitLimit.ShouldBe(1000);
        options.User.PermitLimit.ShouldBe(200);
        options.Ip.PermitLimit.ShouldBe(300);
        options.Auth.PermitLimit.ShouldBe(10);
        options.Tenant.WindowSeconds.ShouldBe(60);
        options.Auth.QueueLimit.ShouldBe(0);
    }

    [Fact]
    public void FixedWindowPolicyOptions_Should_HaveDefaults_When_Constructed()
    {
        // Act
        var policy = new FixedWindowPolicyOptions();

        // Assert
        policy.PermitLimit.ShouldBe(100);
        policy.WindowSeconds.ShouldBe(60);
        policy.QueueLimit.ShouldBe(0);
    }

    #endregion

    #region IdempotencyOptions

    [Fact]
    public void IdempotencyOptions_Should_HaveDefaults_When_Constructed()
    {
        // Act
        var options = new IdempotencyOptions();

        // Assert
        options.HeaderName.ShouldBe("Idempotency-Key");
        options.DefaultTtl.ShouldBe(TimeSpan.FromHours(24));
        options.ReservationTtl.ShouldBe(TimeSpan.FromMinutes(1));
        options.MaxKeyLength.ShouldBe(128);
    }

    // A bad TTL is invisible at runtime: a zero DefaultTtl throws inside the best-effort cache write,
    // which logs a warning and carries on, so nothing is ever stored and replay never engages. It has
    // to be rejected at startup instead.
    // The expected message is part of the case: a zero DefaultTtl also trips the
    // "ReservationTtl must not exceed DefaultTtl" clause, so asserting only the exception type let the
    // DefaultTtl clause be deleted with every row still green.
    [Theory]
    [InlineData("DefaultTtl", "00:00:00", "DefaultTtl must be greater than zero")]
    [InlineData("ReservationTtl", "00:00:00", "ReservationTtl must be greater than zero")]
    [InlineData("ReservationTtl", "48:00:00", "ReservationTtl must not exceed DefaultTtl")]
    [InlineData("MaxKeyLength", "0", "MaxKeyLength must be greater than zero")]
    [InlineData("HeaderName", "", "HeaderName is required")]
    public void AddHeroIdempotency_Should_FailAtStartup_When_OptionsAreInvalid(string key, string value, string expectedFailure)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>($"IdempotencyOptions:{key}", value)])
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddHeroIdempotency(configuration);
        var provider = services.BuildServiceProvider();

        // Act — through IStartupValidator, which is what .ValidateOnStart() registers and what the host
        // runs before serving traffic. Resolving IOptions<>.Value instead would validate lazily and pass
        // with .ValidateOnStart() deleted, moving the failure from boot to the first keyed request.
        var act = () => provider.GetRequiredService<IStartupValidator>().Validate();

        // Assert — the failure has to be the clause this row targets, not any clause that happens to trip
        act.ShouldThrow<OptionsValidationException>()
            .Failures.ShouldContain(failure => failure.Contains(expectedFailure, StringComparison.Ordinal));
    }

    [Fact]
    public void AddHeroIdempotency_Should_Bind_When_OptionsAreValid()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("IdempotencyOptions:ReservationTtl", "00:02:00")])
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddHeroIdempotency(configuration);
        var provider = services.BuildServiceProvider();

        // Act
        provider.GetRequiredService<IStartupValidator>().Validate();
        var options = provider.GetRequiredService<IOptions<IdempotencyOptions>>().Value;

        // Assert — sanity: the validators above reject bad values without rejecting good ones, and the
        // startup validation this configuration passes through does not reject a valid one.
        options.ReservationTtl.ShouldBe(TimeSpan.FromMinutes(2));
    }

    #endregion
}
