namespace DN.WebApi.Application.Settings;

public class MailSettings : IAppSettings
{
    public string From { get; set; }

    public string Host { get; set; }

    public int Port { get; set; }

    public string UserName { get; set; }

    public string Password { get; set; }

    public string DisplayName { get; set; }

    public bool EnableVerification { get; set; }
}