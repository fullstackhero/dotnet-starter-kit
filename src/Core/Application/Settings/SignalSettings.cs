namespace DN.WebApi.Application.Settings;

public class SignalSettings
{
    public class Backplane
    {
        public string Provider { get; set; }
        public string StringConnection { get; set; }
    }

    public bool UseBackplane { get; set; }
}