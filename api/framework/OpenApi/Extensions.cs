using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace FSH.Framework.OpenApi;

public static class Extensions
{
    public static WebApplication UseOpenApiDocumentation(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        return app;
    }
}
