var builder = DistributedApplication.CreateBuilder(args);

// Postgres container + database
var postgres = builder.AddPostgres("postgres").WithDataVolume("fsh-postgres-data").AddDatabase("fsh");

var redis = builder.AddRedis("redis").WithDataVolume("fsh-redis-data");

var papercut = builder.AddPapercutSmtp("papercut");

builder.AddProject<Projects.Playground_Api>("playground-api")
    .WithReference(postgres)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("OpenTelemetryOptions__Exporter__Otlp__Endpoint", "https://localhost:4317")
    .WithEnvironment("OpenTelemetryOptions__Exporter__Otlp__Protocol", "grpc")
    .WithEnvironment("OpenTelemetryOptions__Exporter__Otlp__Enabled", "true")
    .WithEnvironment("DatabaseOptions__Provider", "POSTGRESQL")
    .WithEnvironment("DatabaseOptions__ConnectionString", postgres.Resource.ConnectionStringExpression)
    .WithEnvironment("DatabaseOptions__MigrationsAssembly", "FSH.Playground.Migrations.PostgreSQL")
    .WaitFor(postgres)
    .WithReference(redis)
    .WithEnvironment("CachingOptions__Redis", redis.Resource.ConnectionStringExpression)
    .WithEnvironment("CachingOptions__EnableSsl", "true")
    .WaitFor(redis)
    .WithReference(papercut)
    .WaitFor(papercut);

builder.AddProject<Projects.Playground_Blazor>("playground-blazor");

await builder.Build().RunAsync();
