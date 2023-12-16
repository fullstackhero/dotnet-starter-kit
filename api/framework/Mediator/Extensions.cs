using Microsoft.AspNetCore.Builder;

namespace FSH.WebApi.Framework.Mediator;

public static class Extensions
{
    public static WebApplicationBuilder AddFSHMediator(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder;
    }
}
