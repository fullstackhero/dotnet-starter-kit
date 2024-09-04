using System.Diagnostics.Metrics;
using FSH.Starter.Aspire.ServiceDefaults;

namespace FSH.Starter.WebApi.Setting.EntityCode.Domain;
public static class EntityCodeMetrics
{
    private static readonly Meter Meter = new Meter(MetricsConstants.EntityCodes, "1.0.0");
    public static readonly Counter<int> Created = Meter.CreateCounter<int>("items.created");
    public static readonly Counter<int> Updated = Meter.CreateCounter<int>("items.updated");
    public static readonly Counter<int> Deleted = Meter.CreateCounter<int>("items.deleted");
}

