using FSH.Framework.Blazor.UI;
using FSH.Framework.Blazor.UI.Theme;
using FSH.Playground.Blazor;
using FSH.Playground.Blazor.Components;
using FSH.Playground.Blazor.Services;
using FSH.Playground.Blazor.Services.Api;
using Microsoft.AspNetCore.Components.Server.Circuits;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHeroUI();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpClient();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ITokenStore, InMemoryTokenStore>();
builder.Services.AddScoped<ITokenSessionAccessor, TokenSessionAccessor>();
builder.Services.AddScoped<ITokenAccessor, TokenAccessor>();
builder.Services.AddScoped<CircuitHandler, TokenSessionCircuitHandler>();
builder.Services.AddScoped<BffAuthDelegatingHandler>();

// Tenant theme state service
builder.Services.AddScoped<ITenantThemeState, TenantThemeState>();

var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
                 ?? throw new InvalidOperationException("Api:BaseUrl configuration is missing.");

builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<BffAuthDelegatingHandler>();
    handler.InnerHandler ??= new HttpClientHandler();
    return new HttpClient(handler, disposeHandler: false)
    {
        BaseAddress = new Uri(apiBaseUrl)
    };
});

builder.Services.AddApiClients(builder.Configuration);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Simple health endpoints for ALB/ECS
app.MapGet("/health/ready", () => Results.Ok(new { status = "Healthy" }))
   .AllowAnonymous();

app.MapGet("/health/live", () => Results.Ok(new { status = "Alive" }))
   .AllowAnonymous();

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapBffAuthEndpoints();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
