using FSH.Framework.Logging;
using Microsoft.AspNetCore.Builder;

namespace FSH.Framework;

public static class Extensions
{
    public static WebApplicationBuilder AddFSHFramework(this WebApplicationBuilder builder)
    {
        builder.AddFSHLogging();
        return builder;
    }
}
