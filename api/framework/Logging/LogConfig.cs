namespace FSH.Framework.Logging;

public class LogConfig
{
    public string AppName { get; set; } = "FSH.WebApi";
    public string MinimumLogLevel { get; set; } = "INFORMATION";
    public bool WriteToFile { get; set; }
}
