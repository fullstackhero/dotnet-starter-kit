namespace FL_CRMS_ERP_WEBAPI.Infrastructure.BackgroundJobs;

public class HangfireStorageSettings
{
    public string? StorageProvider { get; set; }
    public string? ConnectionString { get; set; }
}