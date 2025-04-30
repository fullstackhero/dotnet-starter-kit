using FSH.Framework.Infrastructure.Messaging.Events;
using FSH.Framework.Infrastructure.OpenApi;
using FSH.Framework.Tenant;
using FSH.Modules.Auditing;
using FSH.Modules.Common.Infrastructure;
using FSH.Modules.Identity;
using FSH.Modules.Tenant;
using FSH.PlayGround.Api.Extensions;
using Scalar.AspNetCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

builder.AddFshFramework();
builder.Services.AddModules(builder.Configuration);

var assemblies = new Assembly[]
        {
            typeof(TenantModule).Assembly,
            typeof(IdentityModule).Assembly,
            typeof(AuditingModule).Assembly
        };
builder.Services.AddInMemoryEventBus(assemblies);

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    string[] versions = ["v1", "v2"];
    app.MapScalarApiReference(options => options.AddDocuments(versions));
}


app.ConfigureMultiTenantDatabases();
app.ConfigureFshFramework();
app.ConfigureModules();

app.UseHttpsRedirection();
await app.RunAsync();