namespace FSH.Framework.Web.RateLimiting;

public sealed class RateLimitingOptions
{
    public bool Enabled { get; set; } = true;

    public FixedWindowPolicyOptions Tenant { get; set; } = new() { PermitLimit = 1000, WindowSeconds = 60, QueueLimit = 0 };

    public FixedWindowPolicyOptions User { get; set; } = new() { PermitLimit = 200, WindowSeconds = 60, QueueLimit = 0 };

    public FixedWindowPolicyOptions Ip { get; set; } = new() { PermitLimit = 300, WindowSeconds = 60, QueueLimit = 0 };

    public FixedWindowPolicyOptions Auth { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60, QueueLimit = 0 };
}
