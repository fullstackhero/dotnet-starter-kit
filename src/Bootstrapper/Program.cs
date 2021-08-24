using System.Reflection;
using DN.WebApi.Infrastructure.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace DN.WebApi.Bootstrapper
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location))
                    .AddJsonFile("Configs/loggersettings.json", optional: false, reloadOnChange: false)
                    .AddJsonFile("Configs/tenantsettings.json", optional: false, reloadOnChange: false)
                    .AddJsonFile("Configs/jwtsettings.json", optional: false, reloadOnChange: false)
                    .AddJsonFile("Configs/mailsettings.json", optional: false, reloadOnChange: false)
                    .AddJsonFile("Configs/corssettings.json", optional: false, reloadOnChange: false)
                    .AddJsonFile("Configs/tenantsettings.json", optional: false, reloadOnChange: false)
                    .AddJsonFile("Configs/dbsettings.json", optional: false, reloadOnChange: false)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile("appsettings.Development.json", optional: false);

            })
            .UseSerilog()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
    }
}