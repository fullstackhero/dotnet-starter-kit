using Microsoft.Extensions.Configuration;

namespace Framework.Tests.Web;

/// <summary>
/// Loads the shipped appsettings the way the host does and pins what a Production deployment
/// actually gets. This exists because an environment overlay cannot clear a JSON array: an empty
/// <c>[]</c> writes no indices, so the base file's entries survive the overlay and
/// <c>appsettings.Production.json</c> shipping <c>"AllowedOrigins": []</c> left
/// <c>http://localhost:5173</c> and <c>:5174</c> in the trust list of every production deployment.
/// The dev origins therefore live in <c>appsettings.Development.json</c>, and this asserts it stays
/// that way.
/// </summary>
public sealed class ShippedConfigurationTests
{
    private static string HostDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "Host", "FSH.Starter.Api");
            if (File.Exists(Path.Combine(candidate, "appsettings.json")))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate src/Host/FSH.Starter.Api from the test output directory.");
    }

    private static IConfigurationRoot LoadFor(string environment)
    {
        var host = HostDirectory();
        return new ConfigurationBuilder()
            .SetBasePath(host)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: false)
            .Build();
    }

    private static string[] OriginsOf(IConfiguration configuration, string section) =>
        configuration.GetSection(section).Get<string[]>() ?? [];

    [Fact]
    public void Production_Should_TrustNoLocalhostOrigin_When_TheShippedFilesAreLoadedInOrder()
    {
        var configuration = LoadFor("Production");

        OriginsOf(configuration, "FrontendOptions:AllowedOrigins").ShouldBeEmpty();
        OriginsOf(configuration, "CorsOptions:AllowedOrigins").ShouldBeEmpty();
        configuration["FrontendOptions:DefaultOrigin"].ShouldBeNullOrEmpty();
    }

    [Fact]
    public void Development_Should_TrustTheTwoLocalSpas_When_TheShippedFilesAreLoadedInOrder()
    {
        var configuration = LoadFor("Development");

        OriginsOf(configuration, "FrontendOptions:AllowedOrigins")
            .ShouldBe(["http://localhost:5173", "http://localhost:5174"]);
        configuration["FrontendOptions:DefaultOrigin"].ShouldBe("http://localhost:5174");
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public void Environments_Should_ShipAnEmptyRedisKeyPrefix(string environment)
    {
        // CachingOptions:KeyPrefix is opt-in. A shipped default would move the Data Protection key ring
        // of every existing deployment on upgrade (DataProtection-Keys -> {prefix}:DataProtection-Keys),
        // and the new ring is empty: auth cookies and reset tokens stop validating, and webhook signing
        // secrets stored DP-protected in the database can never be unprotected again. The migrator
        // links only appsettings.json + .Development.json, so a Production-only value would also give
        // it a different prefix than the API.
        LoadFor(environment)["CachingOptions:KeyPrefix"].ShouldBeNullOrEmpty();
    }
}
