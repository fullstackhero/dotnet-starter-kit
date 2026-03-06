namespace Spec.Tests;

public class SetupSanityCheckTests
{
    [Fact]
    public void SanityCheck_ShouldPass()
    {
        // This test proves that xUnit, Shouldly, and the Spec.Tests 
        // project are correctly wired into the dotnet test runner.
        true.ShouldBeTrue();
    }
}
