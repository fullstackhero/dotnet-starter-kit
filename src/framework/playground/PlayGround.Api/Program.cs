using FSH.Framework.Auditing.Endpoints;
using FSH.Framework.Identity;
using FSH.Framework.Infrastructure;
using FSH.Framework.Tenant;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.ConfigureFshFramework();
builder.Services.ConfigureTenantModule();
builder.Services.ConfigureIdentityModule();
builder.Services.ConfigureAuditingModule();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    string[] versions = ["v1", "v2"];
    app.MapScalarApiReference(options => options.AddDocuments(versions));
}

app.UseFshMultiTenancy();
app.UseFshFramework();

app.MapIdentityEndpoints();
app.MapAuditingEndpoints();

app.UseHttpsRedirection();
await app.RunAsync();