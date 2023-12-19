namespace FSH.Framework.Infrastructure.Database;
public class DbConfig
{
    public bool UseInMemoryDb { get; set; } = true;
    public string Provider { get; set; } = "postgresql";
    public string ConnectionString { get; set; } = string.Empty;
}
