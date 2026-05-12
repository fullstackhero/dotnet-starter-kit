using FSH.Framework.Web;
using FSH.Framework.Web.Modules;
using FSH.Modules.Auditing;
using FSH.Modules.Identity;
using FSH.Modules.Identity.Contracts.v1.Tokens.TokenGeneration;
using FSH.Modules.Identity.Features.v1.Tokens.TokenGeneration;
using FSH.Modules.Multitenancy;
using FSH.Modules.Multitenancy.Contracts.v1.GetTenantStatus;
using FSH.Modules.Webhooks;
using FSH.Modules.Billing;
using FSH.Modules.Catalog;
using FSH.Modules.Tickets;
using FSH.Modules.Multitenancy.Features.v1.GetTenantStatus;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    static void Require(IConfiguration config, string key)
    {
        if (string.IsNullOrWhiteSpace(config[key]))
        {
            throw new InvalidOperationException($"Missing required configuration '{key}' in Production.");
        }
    }

    var config = builder.Configuration;
    Require(config, "DatabaseOptions:ConnectionString");
    Require(config, "CachingOptions:Redis");
    Require(config, "JwtOptions:SigningKey");
}

builder.Services.AddMediator(o =>
{
    o.ServiceLifetime = ServiceLifetime.Scoped;
    o.Assemblies = [
        typeof(GenerateTokenCommand),
        typeof(GenerateTokenCommandHandler),
        typeof(GetTenantStatusQuery),
        typeof(GetTenantStatusQueryHandler),
        typeof(FSH.Modules.Auditing.Contracts.AuditEnvelope),
        typeof(FSH.Modules.Auditing.Persistence.AuditDbContext),
        typeof(FSH.Modules.Webhooks.Contracts.v1.CreateWebhookSubscription.CreateWebhookSubscriptionCommand),
        typeof(FSH.Modules.Webhooks.WebhooksModule),
        typeof(FSH.Modules.Billing.Contracts.BillingContractsMarker),
        typeof(FSH.Modules.Billing.BillingModule),
        typeof(FSH.Modules.Catalog.Contracts.CatalogContractsMarker),
        typeof(FSH.Modules.Catalog.CatalogModule),
        typeof(FSH.Modules.Tickets.Contracts.TicketsContractsMarker),
        typeof(FSH.Modules.Tickets.TicketsModule),
        typeof(FSH.Modules.Files.Contracts.v1.Commands.RequestUploadUrlCommand),
        typeof(FSH.Modules.Files.FilesModule)];
    // Chat markers will be added in Task 1.7+ once the first command + handler land.
    // The Mediator source generator only recognizes assemblies that actually consume
    // Mediator types (ICommand/IQuery/IHandler implementations), not those that merely
    // declare the package reference in csproj.
});

var moduleAssemblies = new Assembly[]
{
    typeof(IdentityModule).Assembly,
    typeof(MultitenancyModule).Assembly,
    typeof(AuditingModule).Assembly,
    typeof(FSH.Modules.Files.FilesModule).Assembly,
    typeof(WebhooksModule).Assembly,
    typeof(BillingModule).Assembly,
    typeof(CatalogModule).Assembly,
    typeof(TicketsModule).Assembly,
    typeof(FSH.Modules.Chat.ChatModule).Assembly,
};

builder.AddHeroPlatform(o =>
{
    o.EnableCaching = true;
    o.EnableMailing = true;
    o.EnableJobs = true;
    o.EnableQuotas = true;
    o.EnableSse = true;
});

builder.AddModules(moduleAssemblies);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<FSH.Starter.Api.DevSeeding.DevDataSeeder>();
}

var app = builder.Build();

app.UseHeroMultiTenantDatabases();
app.UseHeroPlatform(p =>
{
    p.MapModules = true;
    p.ServeStaticFiles = true;
    p.UseQuotas = true;
    p.MapSseEndpoints = true;
});

app.MapGet("/", () => Results.Ok(new { message = "hello world!" }))
   .WithTags("PlayGround")
   .AllowAnonymous();
await app.RunAsync();