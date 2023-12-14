using Microsoft.AspNetCore.Builder;

namespace Framework;

public static class Extensions
{
    public static WebApplicationBuilder AddFSHLogging(this WebApplicationBuilder builder)
    {
        return builder;
    }
}
