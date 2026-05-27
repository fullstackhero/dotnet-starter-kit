using FSH.Modules.Files.Contracts;
using FSH.Modules.Files.Services;
using NSubstitute;

namespace Files.Tests.Services;

public class FileAccessPolicyRegistryTests
{
    [Fact]
    public void Resolve_Should_ReturnPolicy_When_Registered()
    {
        var p = Substitute.For<IFileAccessPolicy>();
        p.OwnerType.Returns("Product");
        var reg = new FileAccessPolicyRegistry([p]);
        reg.Resolve("Product").ShouldBe(p);
    }

    [Fact]
    public void Resolve_Should_BeCaseInsensitive()
    {
        var p = Substitute.For<IFileAccessPolicy>();
        p.OwnerType.Returns("MyFiles");
        var reg = new FileAccessPolicyRegistry([p]);
        reg.Resolve("MYFILES").ShouldBe(p);
    }

    [Fact]
    public void Resolve_Should_ReturnNull_When_NotRegistered()
    {
        var reg = new FileAccessPolicyRegistry([]);
        reg.Resolve("Unknown").ShouldBeNull();
    }

    [Fact]
    public void Resolve_Should_TakeLastWinsOnDuplicateOwnerType()
    {
        var first = Substitute.For<IFileAccessPolicy>();
        first.OwnerType.Returns("Product");
        var second = Substitute.For<IFileAccessPolicy>();
        second.OwnerType.Returns("Product");

        var reg = new FileAccessPolicyRegistry([first, second]);
        reg.Resolve("Product").ShouldBe(second);
    }
}
