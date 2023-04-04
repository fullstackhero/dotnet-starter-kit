using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

if (args.Length == 0)
{
    Console.WriteLine($"fsh-api requires a command to do something");
    return;
}

var command = args[0];
if (command == "install")
{
    await InstallTemplate();
    return;
}

if (command == "new")
{
    if (args.Length != 2)
    {
        Console.WriteLine($"fsh-api new requires a project name. Ex: fsh-api new <ProjectName>");
        return;
    }

    var projectName = args[1];
    await CreateProject(projectName);
    return;
}

if (command == "make:migration")
{
    if (args.Length != 2)
    {
        Console.WriteLine($"fsh-api make:migration requires a migration name. Ex: fsh make:migration <MigrationName>");
        return;
    }

    var name = args[1];
    await CreateMigration(name);
    return;
}

if (command == "run:migration")
{
    await RunMigration();
    return;
}

async Task InstallTemplate()
{
    var psi = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = "new install FullStackHero.WebAPI.Boilerplate"
    };

    using var proc = Process.Start(psi)!;
    await proc.WaitForExitAsync();
}

async Task CreateProject(string projectName)
{
    Console.WriteLine($"Creating a FSH .NET Web API project at \"./{projectName}\"");
    var psi = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"new fsh-api -n {projectName} -o {projectName}"
    };

    using var proc = Process.Start(psi)!;
    await proc.WaitForExitAsync();
    Console.WriteLine($"fsh-api project {projectName} successfully created.");
    Console.WriteLine($"Application ready! Build something amazing.");
}

async Task CreateMigration(string name)
{
    var psi = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"ef migrations add {name}"
    };

    using var proc = Process.Start(psi)!;
    await proc.WaitForExitAsync();
}

async Task RunMigration()
{
    var psi = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"ef database update"
    };

    using var proc = Process.Start(psi)!;
    await proc.WaitForExitAsync();
}