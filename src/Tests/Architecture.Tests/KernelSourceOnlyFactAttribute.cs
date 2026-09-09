using Xunit;

namespace Architecture.Tests;

/// <summary>
/// A fact that runs only when the kernel is present as source.
/// </summary>
/// <remarks>
/// For checks that read the BuildingBlocks project files directly. A project scaffolded with
/// framework packaging consumes the kernel as FSH.Framework.* NuGet packages and has no
/// src/BuildingBlocks directory, so there is nothing to read - skipping states that plainly
/// instead of passing an assertion that never ran. Assembly-level architecture checks are
/// unaffected and keep running in both modes, since the assemblies come from the packages.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class KernelSourceOnlyFactAttribute : FactAttribute
{
    public KernelSourceOnlyFactAttribute()
    {
        if (!Directory.Exists(Path.Combine(ModuleArchitectureTestsFixture.SolutionRoot, "src", "BuildingBlocks")))
        {
            Skip = "Kernel is consumed as NuGet packages; there are no BuildingBlocks project files to inspect.";
        }
    }
}
