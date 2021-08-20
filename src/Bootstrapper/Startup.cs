using DN.WebApi.Infrastructure.Extensions;

namespace Bootstrapper
{
    public class Startup
    {
        public IConfiguration _config { get; }
        public Startup(IConfiguration configuration)
        {
            _config = configuration;
        }     

        public void ConfigureServices(IServiceCollection services)
        {

            services.AddInfrastructure(_config);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseInfrastructure();
        }
    }
}
