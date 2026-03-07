using System.Threading.Tasks;
using FSH.Tests.Functional.Infrastructure;
using FSH.Tests.Shared.Infrastructure;
using Shouldly;
using Xunit;

namespace Spec.Tests;

public class SetupSanityCheckTests : BaseFunctionalTest
{
    public SetupSanityCheckTests(CustomWebApplicationFactory factory) 
        : base(factory)
    {
    }

    [Fact]
    public void SanityCheck_ShouldPass()
    {
        // This test proves that xUnit, Shouldly, and the Spec.Tests 
        // project are correctly wired into the dotnet test runner
        // and now inherits the Functional Testcontainers infrastructure.
        true.ShouldBeTrue();
    }
}

