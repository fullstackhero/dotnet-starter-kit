using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace FSH.Tests.Shared.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;
    private readonly RedisContainer _redisContainer;

    public CustomWebApplicationFactory()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("fsh_test_b")
            .WithUsername("postgres")
            .WithPassword("fsh_secret_123!")
            .Build();

        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DatabaseOptions:ConnectionString", _dbContainer.GetConnectionString() },
                { "CachingOptions:Redis", _redisContainer.GetConnectionString() },
                { "MultitenancyOptions:RunTenantMigrationsOnStartup", "true" }
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddTransient<FSH.Framework.Jobs.Services.IJobService, TestJobService>();
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _redisContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _dbContainer.DisposeAsync().AsTask();
        await _redisContainer.DisposeAsync().AsTask();
    }
}
