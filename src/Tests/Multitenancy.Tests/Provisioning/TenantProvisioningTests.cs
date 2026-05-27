using FSH.Modules.Multitenancy.Provisioning;

namespace Multitenancy.Tests.Provisioning;

/// <summary>
/// Tests for TenantProvisioning domain entity - tenant provisioning workflow.
/// </summary>
public sealed class TenantProvisioningTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Should_SetTenantId()
    {
        // Arrange
        var tenantId = "tenant-1";
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var provisioning = new TenantProvisioning(tenantId, correlationId);

        // Assert
        provisioning.TenantId.ShouldBe(tenantId);
    }

    [Fact]
    public void Constructor_Should_SetCorrelationId()
    {
        // Arrange
        var tenantId = "tenant-1";
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var provisioning = new TenantProvisioning(tenantId, correlationId);

        // Assert
        provisioning.CorrelationId.ShouldBe(correlationId);
    }

    [Fact]
    public void Constructor_Should_SetStatusToPending()
    {
        // Act
        var provisioning = new TenantProvisioning("tenant-1", Guid.NewGuid().ToString());

        // Assert
        provisioning.Status.ShouldBe(TenantProvisioningStatus.Pending);
    }

    [Fact]
    public void Constructor_Should_SetCreatedUtc()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var provisioning = new TenantProvisioning("tenant-1", Guid.NewGuid().ToString());
        var after = DateTime.UtcNow;

        // Assert
        provisioning.CreatedUtc.ShouldBeGreaterThanOrEqualTo(before);
        provisioning.CreatedUtc.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Constructor_Should_GenerateNewId()
    {
        // Act
        var provisioning = new TenantProvisioning("tenant-1", Guid.NewGuid().ToString());

        // Assert
        provisioning.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Constructor_Should_InitializeNullFields()
    {
        // Act
        var provisioning = new TenantProvisioning("tenant-1", Guid.NewGuid().ToString());

        // Assert
        provisioning.CurrentStep.ShouldBeNull();
        provisioning.Error.ShouldBeNull();
        provisioning.JobId.ShouldBeNull();
        provisioning.StartedUtc.ShouldBeNull();
        provisioning.CompletedUtc.ShouldBeNull();
    }

    [Fact]
    public void Constructor_Should_InitializeEmptySteps()
    {
        // Act
        var provisioning = new TenantProvisioning("tenant-1", Guid.NewGuid().ToString());

        // Assert
        provisioning.Steps.ShouldNotBeNull();
        provisioning.Steps.ShouldBeEmpty();
    }

    #endregion

    #region SetJobId Tests

    [Fact]
    public void SetJobId_Should_SetJobId()
    {
        // Arrange
        var provisioning = new TenantProvisioning("tenant-1", Guid.NewGuid().ToString());
        var jobId = "job-12345";

        // Act
        provisioning.SetJobId(jobId);

        // Assert
        provisioning.JobId.ShouldBe(jobId);
    }

    [Fact]
    public void SetJobId_Should_AllowOverwriting()
    {
        // Arrange
        var provisioning = new TenantProvisioning("tenant-1", Guid.NewGuid().ToString());
        provisioning.SetJobId("job-1");

        // Act
        provisioning.SetJobId("job-2");

        // Assert
        provisioning.JobId.ShouldBe("job-2");
    }

    #endregion

    #region MarkRunning Tests

    [Fact]
    public void MarkRunning_Should_SetStatusToRunning()
    {
        // Arrange
        var provisioning = new TenantProvisioning("tenant-1", Guid.NewGuid().ToString());

        // Act
        provisioning.MarkRunning("Migration");

        // Assert
        provisioning.Status.ShouldBe(TenantProvisioningStatus.Running);
    }

    [Fact]
    public void MarkRunning_Should_SetCurrentStep()
    {
        // Arrange
        var provisioning = new TenantProvisioning("tenant-1", Guid.NewGuid().ToString());

        // Act
        provisioning.MarkRunning("Migration");

        // Assert
        provisioning.CurrentStep.ShouldBe("Migration");
    }

    [Fact]
    public void MarkRunning_Should_SetStartedUtc_OnFirstCall()
    {
        // Arrange
        var provisioning = new TenantProvisioning("tenant-1", Guid.NewGuid().ToString());
        var before = DateTime.UtcNow;

        // Act
        provisioning.MarkRunning("Migration");
        var after = DateTime.UtcNow;

        // Assert
        provisioning.StartedUtc.ShouldNotBeNull();
        provisioning.StartedUtc.Value.ShouldBeGreaterThanOrEqualTo(before);
        provisioning.StartedUtc.Value.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void MarkRunning_Should_NotOverwriteStartedUtc_OnSubsequentCalls()
    {
        // Arrange
        var provisioning = new TenantProvisioning("tenant-1", Guid.NewGuid().ToString());
        provisioning.MarkRunning("Migration");
        var firstStartedUtc = provisioning.StartedUtc;

        // Act - Call again with different step
        provisioning.MarkRunning("Seeding");

        // Assert - StartedUtc should not change
        provisioning.StartedUtc.ShouldBe(firstStartedUtc);
        provisioning.CurrentStep.ShouldBe("Seeding");
    }

    #endregion

    #region MarkCompleted Tests

    [Fact]
    public void MarkCompleted_Should_SetStatusToCompleted()
    {
        // Arrange
        var provisioning = new TenantProvisioning("tenant-1", Guid.NewGuid().ToString());
        provisioning.MarkRunning("Migration");

        // Act
        provisioning.MarkCompleted();

        // Assert
        provisioning.Status.ShouldBe(TenantProvisioningStatus.Completed);
    }

    [Fact]
    public void MarkCompleted_Should_SetCompletedUtc()
    {
        // Arrange
        var provisioning = new TenantProvisioning("tenant-1", Guid.NewGuid().ToString());
        var before = DateTime.UtcNow;

        // Act
        provisioning.MarkCompleted();
        var after = DateTime.UtcNow;

        // Assert
        provisioning.CompletedUtc.ShouldNotBeNull();
        provisioning.CompletedUtc.Value.ShouldBeGreaterThanOrEqualTo(before);
        provisioning.CompletedUtc.Value.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void MarkCompleted_Should_ClearCurrentStep()
    {
        // Arrange
        var provisioning = new TenantProvisioning("tenant-1", Guid.NewGuid().ToString());
        provisioning.MarkRunning("Migration");

        // Act
        provisioning.MarkCompleted();

        // Assert
        provisioning.CurrentStep.ShouldBeNull();
    }

    [Fact]
    public void MarkCompleted_Should_ClearError()
    {
        // Arrange
        var provisioning = new TenantProvisioning("tenant-1", Guid.NewGuid().ToString());
        provisioning.MarkFailed("Migration", "Some error");

        // Act
        provisioning.MarkCompleted();

        // Assert
        provisioning.Error.ShouldBeNull();
    }

    #endregion

    #region MarkFailed Tests

    [Fact]
    public void MarkFailed_Should_SetStatusToFailed()
    {
        // Arrange
        var provisioning = new TenantProvisioning("tenant-1", Guid.NewGuid().ToString());

        // Act
        provisioning.MarkFailed("Migration", "Database connection failed");

        // Assert
        provisioning.Status.ShouldBe(TenantProvisioningStatus.Failed);
    }

    [Fact]
    public void MarkFailed_Should_SetCurrentStep()
    {
        // Arrange
        var provisioning = new TenantProvisioning("tenant-1", Guid.NewGuid().ToString());

        // Act
        provisioning.MarkFailed("Migration", "Database connection failed");

        // Assert
        provisioning.CurrentStep.ShouldBe("Migration");
    }

    [Fact]
    public void MarkFailed_Should_SetError()
    {
        // Arrange
        var provisioning = new TenantProvisioning("tenant-1", Guid.NewGuid().ToString());
        var error = "Database connection failed";

        // Act
        provisioning.MarkFailed("Migration", error);

        // Assert
        provisioning.Error.ShouldBe(error);
    }

    [Fact]
    public void MarkFailed_Should_SetCompletedUtc()
    {
        // Arrange
        var provisioning = new TenantProvisioning("tenant-1", Guid.NewGuid().ToString());
        var before = DateTime.UtcNow;

        // Act
        provisioning.MarkFailed("Migration", "Error");
        var after = DateTime.UtcNow;

        // Assert
        provisioning.CompletedUtc.ShouldNotBeNull();
        provisioning.CompletedUtc.Value.ShouldBeGreaterThanOrEqualTo(before);
        provisioning.CompletedUtc.Value.ShouldBeLessThanOrEqualTo(after);
    }

    #endregion

    #region State Transition Tests

    [Fact]
    public void Provisioning_Should_SupportFullLifecycle()
    {
        // Arrange
        var provisioning = new TenantProvisioning("tenant-1", Guid.NewGuid().ToString());
        provisioning.Status.ShouldBe(TenantProvisioningStatus.Pending);

        // Act & Assert - Running
        provisioning.MarkRunning("Step1");
        provisioning.Status.ShouldBe(TenantProvisioningStatus.Running);

        // Act & Assert - Different step
        provisioning.MarkRunning("Step2");
        provisioning.Status.ShouldBe(TenantProvisioningStatus.Running);
        provisioning.CurrentStep.ShouldBe("Step2");

        // Act & Assert - Completed
        provisioning.MarkCompleted();
        provisioning.Status.ShouldBe(TenantProvisioningStatus.Completed);
    }

    [Fact]
    public void Provisioning_Should_SupportFailureFromRunning()
    {
        // Arrange
        var provisioning = new TenantProvisioning("tenant-1", Guid.NewGuid().ToString());
        provisioning.MarkRunning("Migration");

        // Act
        provisioning.MarkFailed("Migration", "Connection timeout");

        // Assert
        provisioning.Status.ShouldBe(TenantProvisioningStatus.Failed);
        provisioning.CurrentStep.ShouldBe("Migration");
        provisioning.Error.ShouldBe("Connection timeout");
    }

    #endregion
}