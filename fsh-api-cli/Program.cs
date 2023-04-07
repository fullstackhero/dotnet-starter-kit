using System.Diagnostics;

if (args.Length == 0)
{
    Console.WriteLine("Insufficient Parameters. Refer https://fullstackhero.net/dotnet-webapi-boilerplate/general/fsh-api-console");
    return;
}

string command = args[0];
if (command == "install")
{
    await InstallTemplate();
    return;
}

if (command == "new")
{
    if (args.Length != 2)
    {
        Console.WriteLine("Requires a project name. Ex: fsh-api new <ProjectName>");
        return;
    }

    string projectName = args[1];
    await CreateProject(projectName);
    return;
}

async Task InstallTemplate()
{
    Console.WriteLine("Installing fsh-api Templates.");
    var psi = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = "new install FullStackHero.WebAPI.Boilerplate -v=q"
    };

    using var proc = Process.Start(psi)!;
    await proc.WaitForExitAsync();
    Console.WriteLine("Installed the required templates.");
    Console.WriteLine("Get started by using : fsh-api new <ProjectName>.");
}

async Task CreateProject(string projectName)
{
    Console.WriteLine($"Bootstraping FullStackHero .NET Web API project for you at \"./{projectName}\"");
    var psi = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"new fsh-api -n {projectName} -o {projectName} -v=q"
    };
    using var proc = Process.Start(psi)!;
    await proc.WaitForExitAsync();
    Console.WriteLine($"fsh-api project {projectName} successfully created.");
    Console.WriteLine("Application ready! Build something amazing!");
    Console.WriteLine("Refer to documentation at https://fullstackhero.net/dotnet-webapi-boilerplate/general/getting-started");
}