using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore.Stores;
using Finbuckle.MultiTenant.Extensions;
using Finbuckle.MultiTenant.Stores;
using FSH.Framework.Eventing;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Eventing.InMemory;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Eventing.Persistence;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using FSH.Modules.Files.Contracts.Events;
using FSH.Modules.Multitenancy.Data;
using FSH.Modules.Multitenancy.Services;
using FSH.Modules.Webhooks.Data;
using FSH.Modules.Webhooks.Domain;
using FSH.Modules.Webhooks.Services;
using Integration.Tests.Infrastructure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Collections.Concurrent;

namespace Integration.Tests.Tests.Eventing;

[Collection(FshCollectionDefinition.Name)]
public sealed class DedicatedTenantRoutingTests(FshWebApplicationFactory factory)
{
    [Fact]
    public async Task DispatchAsync_Should_WriteConsumerAndInboxOnlyToDedicatedDatabase()
    {
        await using var environment = await RoutingEnvironment.CreateAsync(factory.Services, false);
        var integrationEvent = NewEvent(environment.Tenant.Id);
        environment.EventIds.Add(integrationEvent.Id);

        await environment.DispatchAsync(integrationEvent);

        DatabaseSnapshot dedicated = await ReadSnapshotAsync(environment.DedicatedConnection, integrationEvent);
        DatabaseSnapshot shared = await ReadSnapshotAsync(environment.DefaultConnection, integrationEvent);
        dedicated.ConsumerRows.ShouldBe(1);
        dedicated.InboxRows.ShouldBe(1);
        shared.ConsumerRows.ShouldBe(0);
        shared.InboxRows.ShouldBe(0);
    }

    [Fact]
    public async Task DispatchAsync_Should_LookUpWebhookSubscriptionsInDedicatedDatabase()
    {
        await using var environment = await RoutingEnvironment.CreateAsync(factory.Services, true);
        var integrationEvent = NewEvent(environment.Tenant.Id);
        environment.EventIds.Add(integrationEvent.Id);
        Guid expected = await environment.SeedSubscriptionAsync();

        await environment.DispatchAsync(integrationEvent);

        environment.Recorder.Records.ShouldHaveSingleItem().ShouldBe((environment.Tenant.Id, expected));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_Should_RestorePreviousContext_AfterSuccessOrFailure(bool fail)
    {
        var services = new ServiceCollection();
        services.AddMultiTenant<AppTenantInfo>().WithDistributedCacheStore(TimeSpan.FromMinutes(60));
        services.AddDistributedMemoryCache();
        using ServiceProvider provider = services.BuildServiceProvider();
        var accessor = provider.GetRequiredService<IMultiTenantContextAccessor<AppTenantInfo>>();
        var setter = provider.GetRequiredService<IMultiTenantContextSetter>();
        var previous = new MultiTenantContext<AppTenantInfo>(new AppTenantInfo("source", "source", "source-db", "test@example.test"));
        setter.MultiTenantContext = previous;
        var tenant = new AppTenantInfo("target", "target", "target-db", "test@example.test");
        var store = provider.GetServices<IMultiTenantStore<AppTenantInfo>>().OfType<DistributedCacheStore<AppTenantInfo>>().Single();
        (await store.AddAsync(tenant)).ShouldBeTrue();
        var scope = new FinbuckleEventTenantScope(accessor, setter,
            provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<FinbuckleEventTenantScope>.Instance);

        async Task DispatchAsync() => await scope.ExecuteAsync("target", async _ =>
        {
            await Task.Yield();
            accessor.MultiTenantContext.TenantInfo.ShouldNotBeNull().Id.ShouldBe(tenant.Id);
            accessor.MultiTenantContext.TenantInfo.ConnectionString.ShouldBe(tenant.ConnectionString);
            if (fail) throw new InvalidOperationException("handler failed");
        });

        if (fail) await Should.ThrowAsync<InvalidOperationException>(DispatchAsync);
        else await DispatchAsync();
        accessor.MultiTenantContext.ShouldBeSameAs(previous);
    }

    private static FileFinalizedIntegrationEvent NewEvent(string tenantId)
    {
        Guid id = Guid.CreateVersion7();
        return new(id, DateTime.UtcNow, tenantId, $"corr-{id:N}", "Files", Guid.CreateVersion7(), "Test", null,
            "application/octet-stream", 1, 1);
    }

    private static async Task<DatabaseSnapshot> ReadSnapshotAsync(string connectionString, FileFinalizedIntegrationEvent integrationEvent)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT
                (SELECT COUNT(*) FROM catalog."Brands" WHERE "Name" = @marker),
                (SELECT COUNT(*) FROM framework."InboxMessages" WHERE "Id" = @event AND "HandlerName" = @handler)
            """, connection);
        command.Parameters.AddWithValue("marker", $"dispatch-{integrationEvent.Id:N}");
        command.Parameters.AddWithValue("event", integrationEvent.Id);
        command.Parameters.AddWithValue("handler", typeof(FileEventMarkerHandler).FullName!);
        await using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).ShouldBeTrue();
        return new DatabaseSnapshot(reader.GetInt64(0), reader.GetInt64(1));
    }

    private sealed record DatabaseSnapshot(long ConsumerRows, long InboxRows);

    private sealed class RoutingEnvironment : IAsyncDisposable
    {
        private readonly string _databaseName;
        private bool _databaseCreated;

        private RoutingEnvironment(ServiceProvider provider, string defaultConnection, string dedicatedConnection,
            string databaseName, AppTenantInfo tenant, WebhookRecorder recorder)
        {
            Provider = provider;
            DefaultConnection = defaultConnection;
            DedicatedConnection = dedicatedConnection;
            _databaseName = databaseName;
            Tenant = tenant;
            Recorder = recorder;
        }

        public ServiceProvider Provider { get; }
        public string DefaultConnection { get; }
        public string DedicatedConnection { get; }
        public AppTenantInfo Tenant { get; }
        public WebhookRecorder Recorder { get; }
        public List<Guid> EventIds { get; } = [];
        public EventingDrainTarget DedicatedTarget => new(Tenant.Id, DedicatedConnection);

        public static async Task<RoutingEnvironment> CreateAsync(IServiceProvider host, bool webhooks)
        {
            string defaultConnection = host.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString;
            string suffix = Guid.CreateVersion7().ToString("N");
            string databaseName = $"routing_{suffix}";
            string dedicatedConnection = new NpgsqlConnectionStringBuilder(defaultConnection) { Database = databaseName }.ConnectionString;
            var tenant = new AppTenantInfo($"tenant-{suffix}", $"tenant-{suffix}", dedicatedConnection, "routing@example.test");
            var recorder = new WebhookRecorder();
            ServiceProvider provider = CreateProvider(host, defaultConnection, recorder, webhooks);
            var environment = new RoutingEnvironment(provider, defaultConnection, dedicatedConnection, databaseName, tenant, recorder);
            try
            {
                await ExecuteDatabaseCommandAsync(defaultConnection, $"CREATE DATABASE {QuoteIdentifier(databaseName)}");
                environment._databaseCreated = true;
                using (provider.GetRequiredService<IEventingDrainScope>().Begin(environment.DedicatedTarget))
                using (var scope = provider.CreateScope())
                {
                    await scope.ServiceProvider.GetRequiredService<EventingDbContext>().Database.MigrateAsync();
                    await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
                    if (webhooks) await scope.ServiceProvider.GetRequiredService<WebhookDbContext>().Database.MigrateAsync();
                }
                using (var scope = provider.CreateScope())
                {
                    (await scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>().AddAsync(tenant)).ShouldBeTrue();
                }
                return environment;
            }
            catch
            {
                await environment.DisposeAsync();
                throw;
            }
        }

        public async Task DispatchAsync(IIntegrationEvent integrationEvent)
        {
            using (Provider.GetRequiredService<IEventingDrainScope>().Begin(new EventingDrainTarget(MultitenancyConstants.Root.Id, null)))
            using (var scope = Provider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<IOutboxWriter>().AddAsync(integrationEvent);
                await scope.ServiceProvider.GetRequiredService<OutboxDispatcher>().DispatchAsync();
            }
        }

        public async Task<Guid> SeedSubscriptionAsync()
        {
            using (Provider.GetRequiredService<IEventingDrainScope>().Begin(DedicatedTarget))
            using (var scope = Provider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<WebhookDbContext>();
                var subscription = WebhookSubscription.Create("https://routing.example.test/hook", [nameof(FileFinalizedIntegrationEvent)], "test-hash");
                context.Subscriptions.Add(subscription);
                await context.SaveChangesAsync();
                return subscription.Id;
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                using (var scope = Provider.CreateScope())
                {
                    await scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>().RemoveAsync(Tenant.Id);
                }
                await using var connection = new NpgsqlConnection(DefaultConnection);
                await connection.OpenAsync();
                await using var command = new NpgsqlCommand("""
                    DELETE FROM catalog."Brands" WHERE "Name" = ANY(@markers);
                    DELETE FROM framework."InboxMessages" WHERE "Id" = ANY(@ids);
                    DELETE FROM framework."OutboxMessages" WHERE "Id" = ANY(@ids);
                    """, connection);
                command.Parameters.AddWithValue("markers", EventIds.Select(id => $"dispatch-{id:N}").ToArray());
                command.Parameters.AddWithValue("ids", EventIds.ToArray());
                await command.ExecuteNonQueryAsync();
            }
            finally
            {
                await Provider.DisposeAsync();
                if (_databaseCreated)
                    await ExecuteDatabaseCommandAsync(DefaultConnection, $"DROP DATABASE {QuoteIdentifier(_databaseName)} WITH (FORCE)");
            }
        }

        private static ServiceProvider CreateProvider(IServiceProvider host, string connection, WebhookRecorder recorder, bool webhooks)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EventingOptions:Provider"] = "InMemory",
                ["EventingOptions:UseHostedServiceDispatcher"] = "false"
            }).Build();
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton(host.GetRequiredService<IHostEnvironment>());
            services.AddSingleton(TimeProvider.System);
            services.AddLogging();
            services.Configure<DatabaseOptions>(options =>
            {
                options.Provider = "POSTGRESQL";
                options.ConnectionString = connection;
                options.MigrationsAssembly = "FSH.Starter.Migrations.PostgreSQL";
            });
            services.AddScoped<IScopedDbConnectionProvider, ScopedDbConnectionProvider>();
            services.AddScoped<AmbientDbTransactionRegistry>();
            services.AddDbContext<TenantDbContext>(options => options.UseNpgsql(connection));
            services.AddDistributedMemoryCache();
            services.AddMultiTenant<AppTenantInfo>()
                .WithDistributedCacheStore(TimeSpan.FromMinutes(60))
                .WithStore<EFCoreStore<TenantDbContext, AppTenantInfo>>(ServiceLifetime.Scoped);
            services.AddEventingCore(configuration);
            services.AddHeroDbContext<CatalogDbContext>();
            services.Replace(ServiceDescriptor.Singleton<IEventTenantScope, FinbuckleEventTenantScope>());
            services.Replace(ServiceDescriptor.Singleton<IEventingDrainScope, FinbuckleEventingDrainScope>());
            services.AddScoped<IIntegrationEventHandler<FileFinalizedIntegrationEvent>, FileEventMarkerHandler>();
            if (webhooks)
            {
                services.AddHeroDbContext<WebhookDbContext>();
                services.AddSingleton<IWebhookDispatcher>(recorder);
                services.AddScoped(typeof(IIntegrationEventHandler<>), typeof(WebhookFanoutHandler<>));
            }
            return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        }

        private static string QuoteIdentifier(string identifier)
        {
            using var builder = new NpgsqlCommandBuilder();
            return builder.QuoteIdentifier(identifier);
        }

        private static async Task ExecuteDatabaseCommandAsync(string connectionString, string sql)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
#pragma warning disable CA2100
            await using var command = new NpgsqlCommand(sql, connection);
#pragma warning restore CA2100
            await command.ExecuteNonQueryAsync();
        }
    }

    public sealed class FileEventMarkerHandler(CatalogDbContext context) : IIntegrationEventHandler<FileFinalizedIntegrationEvent>
    {
        public async Task HandleAsync(FileFinalizedIntegrationEvent integrationEvent, CancellationToken ct = default)
        {
            context.Brands.Add(Brand.Create($"dispatch-{integrationEvent.Id:N}", null, null));
            await context.SaveChangesAsync(ct);
        }
    }

    private sealed class WebhookRecorder : IWebhookDispatcher
    {
        public ConcurrentQueue<(string Tenant, Guid Subscription)> Records { get; } = new();

        public Task EnqueueAsync(string tenantId, Guid subscriptionId, string eventType, string payloadJson, CancellationToken cancellationToken = default)
        {
            Records.Enqueue((tenantId, subscriptionId));
            return Task.CompletedTask;
        }
    }
}
