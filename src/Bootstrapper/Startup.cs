using DN.WebApi.Infrastructure.Extensions;
using DN.WebApi.Application.Extensions;
using FluentValidation.AspNetCore;

namespace DN.WebApi.Bootstrapper
{
    public class Startup
    {
        private readonly IConfiguration _config;

        public Startup(IConfiguration config)
        {
            _config = config;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers()
                .AddFluentValidation();
            services
                .AddApplication()
                .AddInfrastructure(_config);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseInfrastructure(_config);
        }
    }
}