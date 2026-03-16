using FSH.Framework.Shared.Multitenancy;
using Hangfire.Server;
using Serilog.Context;

namespace FSH.Framework.Jobs;

public class CorrelationIdJobFilter : IServerFilter
{
    public void OnPerforming(PerformingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = context.GetJobParameter<string>("correlationId");
        if (!string.IsNullOrEmpty(correlationId))
        {
            LogContext.PushProperty("correlation_id", correlationId);
        }

        var tenantInfo = context.GetJobParameter<AppTenantInfo>(MultitenancyConstants.Identifier);
        if (tenantInfo is not null)
        {
            LogContext.PushProperty("tenant_id", tenantInfo.Id);
        }
    }

    public void OnPerformed(PerformedContext context)
    {
    }
}
