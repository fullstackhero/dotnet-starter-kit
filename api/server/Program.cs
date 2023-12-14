using FSH.Framework;
var builder = WebApplication.CreateBuilder(args);
builder.AddFSHFramework();
var app = builder.Build();
app.UseFSHFramework();
app.Run();
