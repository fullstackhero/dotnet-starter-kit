using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;


namespace FSH.WebApi.Framework.Versioning;
internal static class Extensions
{
    public static WebApplicationBuilder AddVersioning(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddApiVersioning(options =>
            {
                options.ReportApiVersions = true;
            }).EnableApiVersionBinding();
        return builder;
    }
}
