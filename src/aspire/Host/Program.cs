var builder = DistributedApplication.CreateBuilder(args);

builder.AddContainer("grafana", "grafana/grafana")
       .WithBindMount("../../../compose/grafana/config", "/etc/grafana", isReadOnly: true)
       .WithBindMount("../../../compose/grafana/dashboards", "/var/lib/grafana/dashboards", isReadOnly: true)
       .WithHttpEndpoint(port: 3000, targetPort: 3000, name: "http");

builder.AddContainer("prometheus", "prom/prometheus")
       .WithBindMount("../../../compose/prometheus", "/etc/prometheus", isReadOnly: true)
       .WithHttpEndpoint(port: 9090, targetPort: 9090);

var username = builder.AddParameter("pg-username", "admin");
var password = builder.AddParameter("pg-password", "admin");

var database = builder.AddPostgres("db", username, password, port: 5432)
    .WithDataVolume()
    .AddDatabase("fullstackhero");

var api = builder.AddProject<Projects.Server>("webapi")
    .WaitFor(database);

var blazor = builder.AddProject<Projects.Client>("blazor");

using var app = builder.Build();

await app.RunAsync();
