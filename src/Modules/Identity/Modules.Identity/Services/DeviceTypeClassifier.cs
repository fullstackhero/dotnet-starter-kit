namespace FSH.Modules.Identity.Services;

/// <summary>
/// Classifies device types based on user agent device family strings.
/// Extracted from SessionService to reduce cyclomatic complexity.
/// </summary>
public static class DeviceTypeClassifier
{
    private const string Desktop = "Desktop";
    private const string Mobile = "Mobile";
    private const string Tablet = "Tablet";

    private static readonly string[] MobileKeywords = ["mobile", "phone", "iphone", "android"];
    private static readonly string[] TabletKeywords = ["tablet", "ipad"];

    /// <summary>
    /// Determines the device type from a user agent device family string.
    /// </summary>
    /// <param name="deviceFamily">The device family string from user agent parsing.</param>
    /// <returns>Device type: "Desktop", "Mobile", or "Tablet".</returns>
    public static string Classify(string? deviceFamily)
    {
        if (string.IsNullOrWhiteSpace(deviceFamily) || deviceFamily == "Other")
        {
            return Desktop;
        }

        var normalized = deviceFamily.ToLowerInvariant();

        if (MobileKeywords.Any(keyword => normalized.Contains(keyword, StringComparison.Ordinal)))
        {
            return Mobile;
        }

        if (TabletKeywords.Any(keyword => normalized.Contains(keyword, StringComparison.Ordinal)))
        {
            return Tablet;
        }

        return Desktop;
    }
}
