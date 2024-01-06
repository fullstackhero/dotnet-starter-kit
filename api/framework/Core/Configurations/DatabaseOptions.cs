namespace FSH.Framework.Core.Configurations;
public class DatabaseOptions
{
    public const string SectionName = "Database";
    public string Provider { get; set; } = "postgresql";
    public string ConnectionString { get; set; } = string.Empty;
}
