using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace FSH.CLI.Services;

/// <summary>
/// Service for fetching FSH release information from GitHub.
/// </summary>
internal sealed class GitHubReleaseService : IDisposable
{
    private const string GitHubApiBase = "https://api.github.com";
    private const string RepoOwner = "fullstackhero";
    private const string RepoName = "dotnet-starter-kit";

    private readonly HttpClient _httpClient;
    private bool _disposed;

    public GitHubReleaseService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(GitHubApiBase),
            DefaultRequestHeaders =
            {
                { "User-Agent", "FSH-CLI" },
                { "Accept", "application/vnd.github+json" }
            }
        };
    }

    /// <summary>
    /// Get the latest release from GitHub.
    /// </summary>
    public async Task<GitHubRelease?> GetLatestReleaseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/repos/{RepoOwner}/{RepoName}/releases/latest",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GitHubRelease>(cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    /// <summary>
    /// Get all releases from GitHub (for version history).
    /// </summary>
    public async Task<IReadOnlyList<GitHubRelease>> GetReleasesAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/repos/{RepoOwner}/{RepoName}/releases?per_page={count}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            return await response.Content.ReadFromJsonAsync<List<GitHubRelease>>(cancellationToken) ?? [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
        catch (TaskCanceledException)
        {
            return [];
        }
    }

    /// <summary>
    /// Get a specific release by tag name.
    /// </summary>
    public async Task<GitHubRelease?> GetReleaseByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/repos/{RepoOwner}/{RepoName}/releases/tags/{tag}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GitHubRelease>(cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    /// <summary>
    /// Fetch the Directory.Packages.props content from a specific tag/branch.
    /// </summary>
    public async Task<string?> GetPackagesPropsAsync(string refName, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use raw.githubusercontent.com for file content
            using var rawClient = new HttpClient();
            var url = $"https://raw.githubusercontent.com/{RepoOwner}/{RepoName}/{refName}/src/Directory.Packages.props";
            
            var response = await rawClient.GetAsync(url, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// GitHub release information.
/// </summary>
internal sealed class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("published_at")]
    public DateTimeOffset PublishedAt { get; set; }

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; set; }

    [JsonPropertyName("draft")]
    public bool Draft { get; set; }

    /// <summary>
    /// Extract version number from tag name (strips 'v' prefix if present).
    /// </summary>
    public string Version => TagName.TrimStart('v', 'V');
}
