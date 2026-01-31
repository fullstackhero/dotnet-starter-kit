using FSH.CLI.Models;

namespace FSH.CLI.Scaffolding;

/// <summary>
/// Simple validation tests for the refactored TemplateEngine
/// </summary>
internal static class TemplateEngineTests
{
    public static void RunValidationTests()
    {
        Console.WriteLine("Running TemplateEngine validation tests...");

        var testOptions = new ProjectOptions
        {
            Name = "TestProject",
            OutputPath = "/tmp/test",
            Type = ProjectType.Api,
            Architecture = ArchitectureStyle.Monolith,
            Database = DatabaseProvider.PostgreSQL,
            IncludeAspire = false,
            IncludeDocker = true,
            IncludeSampleModule = false
        };

        try
        {
            // Test basic template generation
            Console.Write("Testing solution generation... ");
            var solution = TemplateEngine.GenerateSolution(testOptions);
            if (string.IsNullOrEmpty(solution)) throw new InvalidOperationException("Solution generation failed");
            Console.WriteLine("✓");

            Console.Write("Testing API csproj generation... ");
            var apiCsproj = TemplateEngine.GenerateApiCsproj(testOptions);
            if (string.IsNullOrEmpty(apiCsproj)) throw new InvalidOperationException("API csproj generation failed");
            Console.WriteLine("✓");

            Console.Write("Testing AppSettings generation... ");
            var appSettings = TemplateEngine.GenerateAppSettings(testOptions);
            if (string.IsNullOrEmpty(appSettings)) throw new InvalidOperationException("AppSettings generation failed");
            Console.WriteLine("✓");

            Console.Write("Testing static template generation... ");
            var gitignore = TemplateEngine.GenerateGitignore();
            if (string.IsNullOrEmpty(gitignore)) throw new InvalidOperationException("Gitignore generation failed");
            Console.WriteLine("✓");

            Console.Write("Testing template services initialization... ");
            var renderer = TemplateServices.GetRenderer();
            var validator = TemplateServices.GetValidator();
            var cache = TemplateServices.GetCache();
            var loader = TemplateServices.GetLoader();
            var parser = TemplateServices.GetParser();
            if (renderer == null || validator == null || cache == null || loader == null || parser == null)
                throw new InvalidOperationException("Service initialization failed");
            Console.WriteLine("✓");

            Console.WriteLine("All validation tests passed! ✅");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}");
            throw;
        }
    }
}

#if DEBUG
// Uncomment the following to run validation tests during debug builds
// This can be called from Program.cs during development
/*
public static class TemplateEngineTestRunner
{
    [System.Diagnostics.Conditional("DEBUG")]
    public static void RunTests()
    {
        TemplateEngineTests.RunValidationTests();
    }
}
*/
#endif