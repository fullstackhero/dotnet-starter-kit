namespace FSH.Framework.Core.Configurations;
public class LoggerOptions
{
    public const string SectionName = "Logger";
    public string AppName { get; set; } = "FSH.WebApi";
    public string MinimumLevel { get; set; } = "INFORMATION";
    public bool WriteToConsole { get; set; }
    public bool WriteToFile { get; set; }
}
