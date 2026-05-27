using FSH.Modules.Multitenancy.Provisioning;

namespace Multitenancy.Tests.Provisioning;

/// <summary>
/// Tests for TenantProvisioningStep - individual provisioning step tracking.
/// </summary>
public sealed class TenantProvisioningStepTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Should_SetProvisioningId()
    {
        // Arrange
        var provisioningId = Guid.NewGuid();

        // Act
        var step = new TenantProvisioningStep(provisioningId, TenantProvisioningStepName.Database);

        // Assert
        step.ProvisioningId.ShouldBe(provisioningId);
    }

    [Fact]
    public void Constructor_Should_SetStep()
    {
        // Arrange
        var provisioningId = Guid.NewGuid();

        // Act
        var step = new TenantProvisioningStep(provisioningId, TenantProvisioningStepName.Migrations);

        // Assert
        step.Step.ShouldBe(TenantProvisioningStepName.Migrations);
    }

    [Fact]
    public void Constructor_Should_SetStatusToPending()
    {
        // Act
        var step = new TenantProvisioningStep(Guid.NewGuid(), TenantProvisioningStepName.Database);

        // Assert
        step.Status.ShouldBe(TenantProvisioningStatus.Pending);
    }

    [Fact]
    public void Constructor_Should_GenerateNewId()
    {
        // Act
        var step = new TenantProvisioningStep(Guid.NewGuid(), TenantProvisioningStepName.Database);

        // Assert
        step.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Constructor_Should_InitializeNullFields()
    {
        // Act
        var step = new TenantProvisioningStep(Guid.NewGuid(), TenantProvisioningStepName.Database);

        // Assert
        step.Error.ShouldBeNull();
        step.StartedUtc.ShouldBeNull();
        step.CompletedUtc.ShouldBeNull();
    }

    [Theory]
    [InlineData(TenantProvisioningStepName.Database)]
    [InlineData(TenantProvisioningStepName.Migrations)]
    [InlineData(TenantProvisioningStepName.Seeding)]
    [InlineData(TenantProvisioningStepName.CacheWarm)]
    public void Constructor_Should_AcceptAllStepNames(TenantProvisioningStepName stepName)
    {
        // Act
        var step = new TenantProvisioningStep(Guid.NewGuid(), stepName);

        // Assert
        step.Step.ShouldBe(stepName);
    }

    #endregion

    #region MarkRunning Tests

    [Fact]
    public void MarkRunning_Should_SetStatusToRunning()
    {
        // Arrange
        var step = new TenantProvisioningStep(Guid.NewGuid(), TenantProvisioningStepName.Database);

        // Act
        step.MarkRunning();

        // Assert
        step.Status.ShouldBe(TenantProvisioningStatus.Running);
    }

    [Fact]
    public void MarkRunning_Should_SetStartedUtc_OnFirstCall()
    {
        // Arrange
        var step = new TenantProvisioningStep(Guid.NewGuid(), TenantProvisioningStepName.Database);
        var before = DateTime.UtcNow;

        // Act
        step.MarkRunning();
        var after = DateTime.UtcNow;

        // Assert
        step.StartedUtc.ShouldNotBeNull();
        step.StartedUtc.Value.ShouldBeGreaterThanOrEqualTo(before);
        step.StartedUtc.Value.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void MarkRunning_Should_NotOverwriteStartedUtc_OnSubsequentCalls()
    {
        // Arrange
        var step = new TenantProvisioningStep(Guid.NewGuid(), TenantProvisioningStepName.Database);
        step.MarkRunning();
        var firstStartedUtc = step.StartedUtc;

        // Act - Call again
        step.MarkRunning();

        // Assert - StartedUtc should not change (due to ??= operator)
        step.StartedUtc.ShouldBe(firstStartedUtc);
    }

    #endregion

    #region MarkCompleted Tests

    [Fact]
    public void MarkCompleted_Should_SetStatusToCompleted()
    {
        // Arrange
        var step = new TenantProvisioningStep(Guid.NewGuid(), TenantProvisioningStepName.Database);
        step.MarkRunning();

        // Act
        step.MarkCompleted();

        // Assert
        step.Status.ShouldBe(TenantProvisioningStatus.Completed);
    }

    [Fact]
    public void MarkCompleted_Should_SetCompletedUtc()
    {
        // Arrange
        var step = new TenantProvisioningStep(Guid.NewGuid(), TenantProvisioningStepName.Database);
        var before = DateTime.UtcNow;

        // Act
        step.MarkCompleted();
        var after = DateTime.UtcNow;

        // Assert
        step.CompletedUtc.ShouldNotBeNull();
        step.CompletedUtc.Value.ShouldBeGreaterThanOrEqualTo(before);
        step.CompletedUtc.Value.ShouldBeLessThanOrEqualTo(after);
    }

    #endregion

    #region MarkFailed Tests

    [Fact]
    public void MarkFailed_Should_SetStatusToFailed()
    {
        // Arrange
        var step = new TenantProvisioningStep(Guid.NewGuid(), TenantProvisioningStepName.Database);

        // Act
        step.MarkFailed("Connection failed");

        // Assert
        step.Status.ShouldBe(TenantProvisioningStatus.Failed);
    }

    [Fact]
    public void MarkFailed_Should_SetError()
    {
        // Arrange
        var step = new TenantProvisioningStep(Guid.NewGuid(), TenantProvisioningStepName.Database);
        var error = "Database connection timeout";

        // Act
        step.MarkFailed(error);

        // Assert
        step.Error.ShouldBe(error);
    }

    [Fact]
    public void MarkFailed_Should_SetCompletedUtc()
    {
        // Arrange
        var step = new TenantProvisioningStep(Guid.NewGuid(), TenantProvisioningStepName.Database);
        var before = DateTime.UtcNow;

        // Act
        step.MarkFailed("Error");
        var after = DateTime.UtcNow;

        // Assert
        step.CompletedUtc.ShouldNotBeNull();
        step.CompletedUtc.Value.ShouldBeGreaterThanOrEqualTo(before);
        step.CompletedUtc.Value.ShouldBeLessThanOrEqualTo(after);
    }

    #endregion

    #region Lifecycle Tests

    [Fact]
    public void Step_Should_SupportSuccessfulLifecycle()
    {
        // Arrange
        var step = new TenantProvisioningStep(Guid.NewGuid(), TenantProvisioningStepName.Migrations);
        step.Status.ShouldBe(TenantProvisioningStatus.Pending);

        // Act - Running
        step.MarkRunning();
        step.Status.ShouldBe(TenantProvisioningStatus.Running);
        step.StartedUtc.ShouldNotBeNull();

        // Act - Completed
        step.MarkCompleted();
        step.Status.ShouldBe(TenantProvisioningStatus.Completed);
        step.CompletedUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Step_Should_SupportFailureLifecycle()
    {
        // Arrange
        var step = new TenantProvisioningStep(Guid.NewGuid(), TenantProvisioningStepName.Seeding);

        // Act - Running
        step.MarkRunning();
        step.Status.ShouldBe(TenantProvisioningStatus.Running);

        // Act - Failed
        step.MarkFailed("Seeding failed: unique constraint violation");

        // Assert
        step.Status.ShouldBe(TenantProvisioningStatus.Failed);
        step.Error.ShouldNotBeNull();
        step.Error.ShouldContain("unique constraint violation");
        step.CompletedUtc.ShouldNotBeNull();
    }

    #endregion
}