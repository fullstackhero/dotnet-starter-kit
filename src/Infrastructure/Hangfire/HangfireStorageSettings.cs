namespace DN.WebApi.Infrastructure.Hangfire;

public class HangfireStorageSettings
{
    public string? StorageProvider { get; set; }
    public string? ConnectionString { get; set; }
}