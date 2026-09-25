using Microsoft.Extensions.Configuration;

namespace Framework.Tests.Web;

/// <summary>
/// The `If-Match` contract only reaches the endpoint if CORS lets the header through: it is not
/// safelisted, so with <c>CorsOptions:AllowAll = false</c> the browser's preflight decides whether
/// the precondition ever arrives. <c>CorsPolicyTests</c> builds its configuration in memory, so it
/// cannot notice the shipped files dropping the header — and dropping it degrades the feature back
/// to the lost update this PR exists to prevent, silently, with every test still green.
/// </summary>
public sealed class CorsHeaderConfigurationTests
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

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public void AllowedHeaders_Should_CarryIfMatch_When_TheShippedFilesAreLoadedInOrder(string environment)
    {
        var host = HostDirectory();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(host)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: false)
            .Build();

        var headers = configuration.GetSection("CorsOptions:AllowedHeaders").Get<string[]>() ?? [];

        headers.ShouldContain(
            "if-match",
            "the restricted CORS policy strips any header not on this list, so the PUT would never see the precondition");
    }
}
