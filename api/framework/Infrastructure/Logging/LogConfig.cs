namespace FSH.Framework.Infrastructure.Logging;

public class LogConfig
{
    public string AppName { get; set; } = "FSH.WebApi";
    public string MinimumLogLevel { get; set; } = "INFORMATION";
    public bool WriteToConsole { get; set; }
    public bool WriteToFile { get; set; }
}
