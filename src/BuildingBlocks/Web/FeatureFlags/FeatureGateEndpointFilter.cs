using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace FSH.Framework.Web.FeatureFlags;

/// <summary>
/// Endpoint filter that gates access behind a feature flag.
/// Returns 404 Not Found when the feature is disabled.
/// </summary>
public sealed class FeatureGateEndpointFilter(string featureName) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var featureManager = context.HttpContext.RequestServices.GetRequiredService<IFeatureManager>();
        if (!await featureManager.IsEnabledAsync(featureName).ConfigureAwait(false))
        {
            return TypedResults.NotFound();
        }

        return await next(context).ConfigureAwait(false);
    }
}

public static class FeatureGateExtensions
{
    /// <summary>
    /// Gates the endpoint behind a feature flag. Returns 404 when the feature is disabled.
    /// </summary>
    public static RouteHandlerBuilder RequireFeature(this RouteHandlerBuilder builder, string featureName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddEndpointFilter(new FeatureGateEndpointFilter(featureName));
    }
}
