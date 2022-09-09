namespace FSH.WebApi.Infrastructure.Localization;

public class LocalizationSettings
{
    public string? ResourcesPath { get; set; }
    public string[]? SupportedCultures { get; set; }
    public string? DefaultRequestCulture { get; set; }
    public bool? FallbackToParent { get; set; }
}