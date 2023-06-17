using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace FL_CRMS_ERP_WEBAPI.Infrastructure.FileStorage;

internal static class Startup
{
    internal static IApplicationBuilder UseFileStorage(this IApplicationBuilder app) =>
        app.UseStaticFiles(new StaticFileOptions()
        {
            FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "Files")),
            RequestPath = new PathString("/Files")
        });
}