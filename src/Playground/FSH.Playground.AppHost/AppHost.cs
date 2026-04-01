var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("fsh-postgres-data")
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("fsh");

var redis = builder.AddRedis("redis")
    .WithDataVolume("fsh-redis-data")
    .WithLifetime(ContainerLifetime.Persistent);

// API Service
var api = builder.AddProject<Projects.FSH_Api>("fsh-api")
    .WithReference(postgres)
    .WithReference(redis)
    .WaitFor(postgres)
    .WaitFor(redis)
    .WithExternalHttpEndpoints()
    .WithEnvironment("DatabaseOptions__Provider", "POSTGRESQL")
    .WithEnvironment("DatabaseOptions__ConnectionString", postgres.Resource.ConnectionStringExpression)
    .WithEnvironment("DatabaseOptions__MigrationsAssembly", "FSH.Migrations.PostgreSQL")
    .WithEnvironment("CachingOptions__Redis", redis.Resource.ConnectionStringExpression);

// Blazor UI
builder.AddProject<Projects.Playground_Blazor>("playground-blazor")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints()
    .WithEnvironment("Api__BaseUrl", api.GetEndpoint("http"));

await builder.Build().RunAsync();
