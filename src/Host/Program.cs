using DN.WebApi.Application.DependencyInjection;
using DN.WebApi.Host.Controllers.Catalog;
using DN.WebApi.Host.Controllers.Dashboard;
using DN.WebApi.Host.Controllers.Identity;
using DN.WebApi.Host.Controllers.Multitenancy;
using DN.WebApi.Host.Extensions;
using DN.WebApi.Infrastructure.DependencyInjection;
using FluentValidation.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
Log.Information("Server Booting Up...");
try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.AddConfigurations();
    builder.Host.UseSerilog((_, config) => config.WriteTo.Console().ReadFrom.Configuration(builder.Configuration));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddControllers().AddFluentValidation();

    var app = builder.Build();

    app.UseInfrastructure(builder.Configuration);
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapGrpcService<TokensControllerGrpc>();
        endpoints.MapGrpcService<TenantsControllerGrpc>();
        endpoints.MapGrpcService<UsersControllerGrpc>();
        endpoints.MapGrpcService<RolesControllerGrpc>();
        endpoints.MapGrpcService<RoleClaimsControllerGrpc>();
        endpoints.MapGrpcService<IdentityControllerGrpc>();
        endpoints.MapGrpcService<AuditLogsControllerGrpc>();
        endpoints.MapGrpcService<StatsControllerGrpc>();
        endpoints.MapGrpcService<ProductsControllerGrpc>();
        endpoints.MapGrpcService<BrandsControllerGrpc>();
    });
    app.Run();
}
catch (Exception ex) when (!ex.GetType().Name.Equals("StopTheHostException", StringComparison.Ordinal))
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Server Shutting down...");
    Log.CloseAndFlush();
}