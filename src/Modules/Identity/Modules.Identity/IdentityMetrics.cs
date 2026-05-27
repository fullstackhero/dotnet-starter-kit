using System.Diagnostics.Metrics;

namespace FSH.Modules.Identity;

public sealed class IdentityMetrics : IDisposable
{
    public const string MeterName = "FSH.Modules.Identity";
    private readonly Counter<long> _tokensGenerated;
    private readonly Meter _meter;

    public IdentityMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _tokensGenerated = _meter.CreateCounter<long>(
            name: "identity_tokens_generated",
            unit: "tokens",
            description: "Number of tokens generated");
    }

    public void TokenGenerated(string emailId)
    {
        _tokensGenerated.Add(1, new KeyValuePair<string, object?>("user.email", emailId));
    }

    public void Dispose()
    {
        _meter?.Dispose();
    }
}