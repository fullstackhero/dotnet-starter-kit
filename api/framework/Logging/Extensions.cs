using Microsoft.AspNetCore.Builder;

namespace Framework.Logging;

public static class Extensions
{
    public static WebApplicationBuilder AddFSHLogging(this WebApplicationBuilder builder)
    {
        return builder;
    }
}
