namespace FSH.Framework.Web.OpenApi;

public sealed class OpenApiOptions
{
    public required string Title { get; init; }
    public string Version { get; init; } = "v1";
    public required string Description { get; init; }

    /// <summary>
    /// API versions to generate OpenAPI documents for (e.g., ["v1", "v2"]).
    /// Defaults to ["v1"] if not specified.
    /// </summary>
    public string[]? Versions { get; init; }

    public ContactOptions? Contact { get; init; }
    public LicenseOptions? License { get; init; }

    public sealed class ContactOptions
    {
        public string? Name { get; init; }
        public Uri? Url { get; init; }
        public string? Email { get; init; }
    }

    public sealed class LicenseOptions
    {
        public string? Name { get; init; }
        public Uri? Url { get; init; }
    }
}