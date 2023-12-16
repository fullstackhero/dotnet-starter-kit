using FSH.Framework;
using FSH.WebApi.Server;
var builder = WebApplication.CreateBuilder(args);
builder.AddFSH();
builder.AddModules();
var app = builder.Build();
app.UseFSH();
app.UseModules();
app.Run();
