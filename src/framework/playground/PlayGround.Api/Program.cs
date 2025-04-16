using FSH.Framework.Identity;
using FSH.Framework.Infrastructure;
using FSH.Framework.Tenant;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.ConfigureFshFramework();
builder.Services.RegisterTenantModuleServices();
builder.Services.RegisterIdentityModule();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
app.UseTenantModule();
app.MapIdentityEndpoints();
app.UseFshFramework();
app.MapGet("/", () => "Gello");
app.UseHttpsRedirection();
await app.RunAsync();