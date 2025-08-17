namespace FSH.Starter.Api;
public class PaymentsOptions
{
    public const string Section = "Payments";
    public string Provider { get; set; } = "Razorpay"; // Razorpay | Stripe
    public string Key { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
}
