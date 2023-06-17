namespace FL_CRMS_ERP_WEBAPI.Infrastructure.Logging;

public class LoggerSettings
{
    public string AppName { get; set; } = "FL_CRMS_ERP_WEBAPI";
    public string ElasticSearchUrl { get; set; } = string.Empty;
    public bool WriteToFile { get; set; } = false;
    public bool StructuredConsoleLogging { get; set; } = false;
    public string MinimumLogLevel { get; set; } = "Information";
}
