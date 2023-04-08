using System.Diagnostics;

if (args.Length == 0)
{
    Console.WriteLine("insufficient params. pleae refer to https://fullstackhero.net/dotnet-webapi-boilerplate/general/fsh-api-console");
    return;
}

string firstArg = args[0];
if (firstArg == "install" || firstArg == "i" || firstArg == "update" || firstArg == "u")
{
    await InstallTemplates();
    return;
}

if (firstArg == "api")
{
    if (args.Length != 3)
    {
        Console.WriteLine("invalid command. use something like : fsh api new <projectname>");
        return;
    }

    string command = args[1];
    string projectName = args[2];
    if (command == "n" || command == "new")
    {
        await BootstrapWebApiSolution(projectName);
    }

    return;
}

if (firstArg == "wasm")
{
    if (args.Length != 3)
    {
        Console.WriteLine("invalid command. use something like : fsh wasm new <projectname>");
        return;
    }

    string command = args[1];
    string projectName = args[2];
    if (command == "n" || command == "new")
    {
        await BootstrapBlazorWasmSolution(projectName);
    }

    return;
}

async Task InstallTemplates()
{
    WriteSuccessMessage("installing fsh dotnet webapi template...");
    var apiPsi = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = "new install FullStackHero.WebAPI.Boilerplate"
    };
    using var apiProc = Process.Start(apiPsi)!;
    await apiProc.WaitForExitAsync();

    Console.WriteLine("installing fsh blazor wasm template...");
    var wasmPsi = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = "new install FullStackHero.BlazorWebAssembly.Boilerplate"
    };
    using var wasmProc = Process.Start(wasmPsi)!;
    await wasmProc.WaitForExitAsync();

    WriteSuccessMessage("installed the required templates.");
    Console.WriteLine("get started by using : fsh <type> new <projectname>.");
    Console.WriteLine("note : <type> can be api, wasm.");
    Console.WriteLine("refer to documentation at https://fullstackhero.net/dotnet-webapi-boilerplate/general/getting-started");
}

async Task BootstrapWebApiSolution(string projectName)
{
    Console.WriteLine($"bootstraping fullstackhero dotnet webapi project for you at \"./{projectName}\"...");
    var psi = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"new fsh-api -n {projectName} -o {projectName} -v=q"
    };
    using var proc = Process.Start(psi)!;
    await proc.WaitForExitAsync();
    WriteSuccessMessage($"fsh-api project {projectName} successfully created.");
    WriteSuccessMessage("application ready! build something amazing!");
    Console.WriteLine("refer to documentation at https://fullstackhero.net/dotnet-webapi-boilerplate/general/getting-started");
}

async Task BootstrapBlazorWasmSolution(string projectName)
{
    Console.WriteLine($"bootstraping fullstackhero blazor wasm solution for you at \"./{projectName}\"...");
    var psi = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"new fsh-blazor -n {projectName} -o {projectName} -v=q"
    };
    using var proc = Process.Start(psi)!;
    await proc.WaitForExitAsync();
    WriteSuccessMessage($"fullstackhero blazor wasm solution {projectName} successfully created.");
    WriteSuccessMessage("application ready! build something amazing!");
    Console.WriteLine("refer to documentation at https://fullstackhero.net/blazor-webassembly-boilerplate/general/getting-started/");
}

void WriteSuccessMessage(string message)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine(message);
    Console.ResetColor();
}