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
            services
                .AddOptions<S3StorageOptions>()
                .Bind(configuration.GetSection("Storage:S3"))
                .Validate(o => string.IsNullOrWhiteSpace(o.PresignServiceUrl) || IsRootHttpUrl(o.PresignServiceUrl), "Storage:S3:PresignServiceUrl must be an absolute http(s) URL with no path, e.g. https://s3.example.com.")
                .Validate(o => !PrefixStartsWithVisibilityRoot(o.Prefix), "Storage:S3:Prefix must not start with 'public' or 'private': keys are stored as {Prefix}/public/... and {Prefix}/private/..., and a public/* read policy would then cover private files.")
                .ValidateOnStart();

            services.AddSingleton<IAmazonS3>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<S3StorageOptions>>().Value;

                if (string.IsNullOrWhiteSpace(options.Bucket))
                {
                    throw new InvalidOperationException("Storage:S3:Bucket is required when using S3 storage.");
                }

                return CreateS3Client(options, options.ServiceUrl);
            });

            // A second client only so presigned URLs are signed for the host browsers use. Signing itself is
            // offline, and without explicit keys the only call this client makes is the one-time fetch of
            // ambient credentials. All real I/O stays on the client above.
            services.AddKeyedSingleton<IAmazonS3>(S3StorageService.PresignClientKey, (sp, _) =>
            {
                var options = sp.GetRequiredService<IOptions<S3StorageOptions>>().Value;
                return CreateS3Client(options, options.PresignEndpoint);
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

    private static AmazonS3Client CreateS3Client(S3StorageOptions options, string? serviceUrl)
    {
        var config = new AmazonS3Config();

        if (!string.IsNullOrWhiteSpace(serviceUrl))
        {
            // S3-compatible endpoint (e.g. MinIO). Path-style addressing is typically required
            // because these services don't route virtual-hosted-style bucket subdomains.
            config.ServiceURL = serviceUrl;
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
    }

    // A path would be signed into every URL (https://host/s3/bucket/key): a proxy that strips it breaks
    // the signature, and one that keeps it makes the store read "s3" as the bucket.
    private static bool IsRootHttpUrl(string value) =>
        Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && uri.AbsolutePath == "/"
        && string.IsNullOrEmpty(uri.Query)
        && string.IsNullOrEmpty(uri.Fragment);

    // Keys are {Prefix}/{public|private}/…. A prefix whose first segment is itself a visibility root puts
    // private keys under public/ (or the other way round), where the bucket's public/* read policy applies.
    private static bool PrefixStartsWithVisibilityRoot(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return false;
        }

        var firstSegment = prefix.Trim().TrimStart('/').Split('/', 2)[0];
        return string.Equals(firstSegment + "/", StorageVisibilityRoot.Public, StringComparison.OrdinalIgnoreCase)
            || string.Equals(firstSegment + "/", StorageVisibilityRoot.Private, StringComparison.OrdinalIgnoreCase);
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
