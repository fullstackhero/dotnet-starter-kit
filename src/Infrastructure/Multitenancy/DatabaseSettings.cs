namespace DN.WebApi.Infrastructure.Multitenancy;

public class DatabaseSettings
{
    public string? DBProvider { get; set; }
    public string? ConnectionString { get; set; }
}