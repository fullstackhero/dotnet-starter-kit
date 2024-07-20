var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Server>("webapi");
builder.AddProject<Projects.Client>("blazor");


await builder.Build().RunAsync();
