using FSH.Modules.Multitenancy.Provisioning;

namespace Multitenancy.Tests.Provisioning;

/// <summary>
/// Tests for TenantProvisioningStatus enum values.
/// </summary>
public sealed class TenantProvisioningStatusTests
{
    [Fact]
    public void Pending_Should_BeZero()
    {
        // Assert
        ((int)TenantProvisioningStatus.Pending).ShouldBe(0);
    }

    [Fact]
    public void Running_Should_BeOne()
    {
        // Assert
        ((int)TenantProvisioningStatus.Running).ShouldBe(1);
    }

    [Fact]
    public void Completed_Should_BeTwo()
    {
        // Assert
        ((int)TenantProvisioningStatus.Completed).ShouldBe(2);
    }

    [Fact]
    public void Failed_Should_BeThree()
    {
        // Assert
        ((int)TenantProvisioningStatus.Failed).ShouldBe(3);
    }

    [Fact]
    public void Enum_Should_HaveFourValues()
    {
        // Act
        var values = Enum.GetValues<TenantProvisioningStatus>();

        // Assert
        values.Length.ShouldBe(4);
    }

    [Theory]
    [InlineData(TenantProvisioningStatus.Pending, "Pending")]
    [InlineData(TenantProvisioningStatus.Running, "Running")]
    [InlineData(TenantProvisioningStatus.Completed, "Completed")]
    [InlineData(TenantProvisioningStatus.Failed, "Failed")]
    public void Enum_Should_HaveCorrectNames(TenantProvisioningStatus status, string expectedName)
    {
        // Assert
        status.ToString().ShouldBe(expectedName);
    }
}