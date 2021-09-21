using DN.WebApi.Application.Extensions;
using DN.WebApi.Infrastructure.Extensions;
using DN.WebApi.Shared.DTOs;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            MapsterSettings.Configure();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseInfrastructure(_config);
        }
    }
}