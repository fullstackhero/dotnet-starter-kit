namespace FSH.Framework.Web.Cors;

public sealed class CorsOptions
{
    public bool AllowAll { get; init; } = true;
    public string[] AllowedOrigins { get; init; } = [];
    public string[] AllowedHeaders { get; init; } = ["*"];
    public string[] AllowedMethods { get; init; } = ["*"];
}