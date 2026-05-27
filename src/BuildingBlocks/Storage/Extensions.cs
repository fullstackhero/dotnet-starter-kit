using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Quota;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Storage.Local;
using FSH.Framework.Storage.S3;
using FSH.Framework.Storage.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Storage;

public static class Extensions
{
    public static IServiceCollection AddHeroLocalFileStorage(this IServiceCollection services)
    {
        services.AddScoped<IStorageService, LocalStorageService>();
        return services;
    }

    public static IServiceCollection AddHeroStorage(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = configuration["Storage:Provider"]?.ToLowerInvariant();
        var quotaEnabled = configuration
            .GetSection(nameof(QuotaOptions))
            .Get<QuotaOptions>()?.Enabled == true;

        if (string.Equals(provider, "s3", StringComparison.OrdinalIgnoreCase))
        {
            services.Configure<S3StorageOptions>(configuration.GetSection("Storage:S3"));

            services.AddSingleton<IAmazonS3>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<S3StorageOptions>>().Value;

                if (string.IsNullOrWhiteSpace(options.Bucket))
                {
                    throw new InvalidOperationException("Storage:S3:Bucket is required when using S3 storage.");
                }

                var config = new AmazonS3Config();

                if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
                {
                    // S3-compatible endpoint (e.g. MinIO). Path-style addressing is typically required
                    // because these services don't route virtual-hosted-style bucket subdomains.
                    config.ServiceURL = options.ServiceUrl;
                    config.ForcePathStyle = options.ForcePathStyle;

                    // The SDK still wants an auth region for SigV4 even when hitting a custom endpoint.
                    config.AuthenticationRegion = string.IsNullOrWhiteSpace(options.Region) ? "us-east-1" : options.Region;
                }
                else if (!string.IsNullOrWhiteSpace(options.Region))
                {
                    config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
                }

                var hasExplicitCredentials = !string.IsNullOrWhiteSpace(options.AccessKey)
                    && !string.IsNullOrWhiteSpace(options.SecretKey);

                return hasExplicitCredentials
                    ? new AmazonS3Client(new BasicAWSCredentials(options.AccessKey, options.SecretKey), config)
                    : new AmazonS3Client(config);
            });

            services.AddTransient<S3StorageService>();
            RegisterStorageService<S3StorageService>(services, quotaEnabled, ServiceLifetime.Transient);
        }
        else
        {
            services.AddScoped<LocalStorageService>();
            RegisterStorageService<LocalStorageService>(services, quotaEnabled, ServiceLifetime.Scoped);
        }

        return services;
    }

    private static void RegisterStorageService<TInner>(
        IServiceCollection services,
        bool quotaEnabled,
        ServiceLifetime innerLifetime)
        where TInner : class, IStorageService
    {
        if (quotaEnabled)
        {
            // The decorator's lifetime is scoped because IQuotaService resolves per-request.
            services.AddScoped<IStorageService>(sp => new QuotaMeteredStorageService(
                sp.GetRequiredService<TInner>(),
                sp.GetRequiredService<IQuotaService>(),
                sp.GetRequiredService<IMultiTenantContextAccessor<AppTenantInfo>>(),
                sp.GetRequiredService<ILogger<QuotaMeteredStorageService>>()));
            return;
        }

        services.Add(new ServiceDescriptor(
            typeof(IStorageService),
            sp => sp.GetRequiredService<TInner>(),
            innerLifetime));
    }
}
