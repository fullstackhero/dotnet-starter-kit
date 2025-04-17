using FSH.Framework.Auditing.Endpoints;
using FSH.Framework.Identity;
using FSH.Framework.Infrastructure;
using FSH.Framework.Infrastructure.Messaging.Events;
using FSH.Framework.Infrastructure.OpenApi;
using FSH.Framework.Tenant;
using Scalar.AspNetCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});
builder.ConfigureFshFramework();
builder.Services.ConfigureTenantModule();
builder.Services.ConfigureIdentityModule();
builder.Services.ConfigureAuditingModule();

var assemblies = new Assembly[]
        {
            typeof(TenantModule).Assembly,
            typeof(IdentityModule).Assembly,
            typeof(AuditingModule).Assembly
        };
builder.Services.RegisterInMemoryEventBus(assemblies);

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