using FSH.Starter.Api.Data;
using FSH.Starter.Api.Services;
using Microsoft.EntityFrameworkCore;
using FSH.Framework.Infrastructure;
using FSH.Framework.Infrastructure.Logging.Serilog;
using FSH.Starter.WebApi.Host;
using Serilog;

StaticLogger.EnsureInitialized();
Log.Information("server booting up..");
try
{
    var builder = WebApplication.CreateBuilder(args);
    // --- Chatbot registrations ---
    builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection(OpenAiOptions.Section));
    builder.Services.Configure<WhatsAppOptions>(builder.Configuration.GetSection(WhatsAppOptions.Section));
    builder.Services.Configure<PaymentsOptions>(builder.Configuration.GetSection(PaymentsOptions.Section));

    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        var cs = builder.Configuration.GetConnectionString("ChatbotDb");
        if (!string.IsNullOrWhiteSpace(cs)) options.UseNpgsql(cs);
        else options.UseSqlite("Data Source=chatbot.db");
    });

    builder.Services.AddHttpClient<OpenAiLlmService>();
    builder.Services.AddScoped<ILlmService, OpenAiLlmService>();
    builder.Services.AddScoped<IChatService, ChatService>();
    builder.Services.AddScoped<IQuotaService, QuotaService>();
    builder.Services.AddWhatsAppProvider(builder.Configuration);

    builder.ConfigureFshFramework();
    builder.RegisterModules();

    var app = builder.Build();

    // Create schema (dev convenience)
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
    }

    app.UseFshFramework();
    app.UseModules();
    await app.RunAsync();
}
catch (Exception ex) when (!ex.GetType().Name.Equals("HostAbortedException", StringComparison.Ordinal))
{
    StaticLogger.EnsureInitialized();
    Log.Fatal(ex.Message, "unhandled exception");
}
finally
{
    StaticLogger.EnsureInitialized();
    Log.Information("server shutting down..");
    await Log.CloseAndFlushAsync();
}
