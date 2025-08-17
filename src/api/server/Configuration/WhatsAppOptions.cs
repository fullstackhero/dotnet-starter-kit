namespace FSH.Starter.Api;
public class WhatsAppOptions
{
    public const string Section = "WhatsApp";
    public string Provider { get; set; } = "Twilio"; // Twilio | Stub
    public string FromNumber { get; set; } = string.Empty;
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty; // for Meta, if you add later
}
