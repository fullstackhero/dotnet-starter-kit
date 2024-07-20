var builder = DistributedApplication.CreateBuilder(args);

var grafana = builder.AddContainer("grafana", "grafana/grafana")
       .WithBindMount("./grafana/config", "/etc/grafana", isReadOnly: true)
       .WithBindMount("./grafana/dashboards", "/var/lib/grafana/dashboards", isReadOnly: true)
       .WithHttpEndpoint(port: 3000, targetPort: 3000, name: "http");

builder.AddContainer("prometheus", "prom/prometheus")
       .WithBindMount("./prometheus", "/etc/prometheus", isReadOnly: true)
       .WithHttpEndpoint(port: 9090, targetPort: 9090);

builder.AddProject<Projects.Server>("webapi");

builder.AddProject<Projects.Client>("blazor");

using var app = builder.Build();

await app.RunAsync();
