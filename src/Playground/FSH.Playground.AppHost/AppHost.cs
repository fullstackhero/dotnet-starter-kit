using System.Net.Sockets;

var builder = DistributedApplication.CreateBuilder(args);

// Postgres container + database
var postgres = builder.AddPostgres("postgres").WithDataVolume("fsh-postgres-data").AddDatabase("fsh");

var redis = builder.AddRedis("redis").WithDataVolume("fsh-redis-data");

var mailpit = builder.AddContainer("mailpit", "axllent/mailpit", "latest")
    .WithEndpoint("smtp", e =>
    {
        e.TargetPort = 1025;   //  // container port
        e.Port = 1025;         // host port
        e.Protocol = ProtocolType.Tcp;
        e.UriScheme = "smtp";
    })
    .WithEndpoint("ui", e =>
    {
        e.TargetPort = 8025;  // ui container
        e.Port = 8025;
        e.UriScheme = "http";
    });

var papercut = builder.AddContainer("papercut", "jijiechen/papercut", "latest")
  .WithEndpoint("smtp", e =>
  {
      e.TargetPort = 25;   // container port
      e.Port = 25;         // host port
      e.Protocol = ProtocolType.Tcp;
      e.UriScheme = "smtp";
  })
  .WithEndpoint("ui", e =>
  {
      e.TargetPort = 37408;
      e.Port = 37408;
      e.UriScheme = "http";
  });

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
    .WaitFor(papercut)
    .WaitFor(mailpit);

builder.AddProject<Projects.Playground_Blazor>("playground-blazor");

await builder.Build().RunAsync();
