using FSH.Framework.Identity;
using FSH.Framework.Infrastructure;
using FSH.Framework.Tenant;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.ConfigureFshFramework();
builder.Services.ConfigureTenantModule();
builder.Services.ConfigureIdentityModule();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseFshMultiTenancy();
app.UseFshFramework();

app.MapIdentityEndpoints();

app.UseHttpsRedirection();
await app.RunAsync();