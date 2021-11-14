using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DN.WebApi.Bootstrapper")]

namespace DN.WebApi.Infrastructure.Extensions
{
    internal static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder app, IConfiguration config)
        {
            var options = new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(new CultureInfo("en-US"))
            };
            app.UseRequestLocalization(options);
            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "Files")),
                RequestPath = new PathString("/Files")
            });
            app.UseMiddlewares(config);
            app.UseRouting();
            app.UseCors("CorsPolicy");
            app.UseAuthentication();
            app.UseAuthorization();

            var configDashboard = config.GetSection("HangFireSettings:Dashboard").Get<DashboardOptions>();
            app.UseHangfireDashboard(config["HangFireSettings:Route"], new DashboardOptions
            {
                DashboardTitle = configDashboard.DashboardTitle,
                StatsPollingInterval = configDashboard.StatsPollingInterval,
                AppPath = configDashboard.AppPath

                // ** OPtional BasicAuthAuthorizationFilter **
                // Authorization = new[] { new BasicAuthAuthorizationFilter(
                //    new BasicAuthAuthorizationFilterOptions {
                //        RequireSsl = false,
                //        SslRedirect = false,
                //        LoginCaseSensitive = true,
                //        Users = new []
                //        {
                //            new BasicAuthAuthorizationUser
                //            {
                //                Login = config["HangFireSettings:Credentiales:User"],
                //                PasswordClear =  config["HangFireSettings:Credentiales:Password"]
                //            }
                //        }
                //    })
                // }

            });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers().RequireAuthorization();
                endpoints.MapHealthChecks("/health").RequireAuthorization();
            });
            app.UseSwaggerDocumentation(config);
            return app;
        }
    }
}