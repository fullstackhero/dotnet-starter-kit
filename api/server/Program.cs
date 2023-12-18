using FSH.Framework;
using FSH.WebApi.Server;

var builder = WebApplication.CreateBuilder(args);
builder.AddFSHFramework();
builder.AddModules();

var app = builder.Build();
app.UseFSHFramework();
app.UseModules();
app.Run();
