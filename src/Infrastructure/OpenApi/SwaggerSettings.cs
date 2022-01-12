namespace FSH.WebApi.Infrastructure.OpenApi;

public class SwaggerSettings
{
    public bool Enable { get; set; }
    public string? Title { get; set; }
    public string? Version { get; set; }
    public string? Description { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactUrl { get; set; }
    public bool License { get; set; }
    public string? LicenseName { get; set; }
    public string? LicenseUrl { get; set; }
}