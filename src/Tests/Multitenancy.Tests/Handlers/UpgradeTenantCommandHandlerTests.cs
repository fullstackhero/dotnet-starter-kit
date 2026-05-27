using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Contracts.v1.UpgradeTenant;
using FSH.Modules.Multitenancy.Features.v1.UpgradeTenant;
using NSubstitute;

namespace Multitenancy.Tests.Handlers;

/// <summary>
/// Tests for UpgradeTenantCommandHandler - handles tenant subscription upgrades.
/// </summary>
public sealed class UpgradeTenantCommandHandlerTests
{
    private readonly ITenantService _tenantService;
    private readonly UpgradeTenantCommandHandler _sut;

    public UpgradeTenantCommandHandlerTests()
    {
        _tenantService = Substitute.For<ITenantService>();
        _sut = new UpgradeTenantCommandHandler(_tenantService);
    }

    #region Handle Tests

    [Fact]
    public async Task Handle_Should_CallUpgradeSubscriptionAsync_WithCorrectParameters()
    {
        // Arrange
        var tenantId = "tenant-1";
        var extendedExpiryDate = DateTime.UtcNow.AddYears(1);
        var command = new UpgradeTenantCommand(tenantId, extendedExpiryDate);

        _tenantService.UpgradeSubscriptionAsync(tenantId, extendedExpiryDate, Arg.Any<CancellationToken>())
            .Returns(extendedExpiryDate);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _tenantService.Received(1)
            .UpgradeSubscriptionAsync(tenantId, extendedExpiryDate, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnCorrectResponse()
    {
        // Arrange
        var tenantId = "tenant-1";
        var extendedExpiryDate = DateTime.UtcNow.AddYears(1);
        var returnedValidity = extendedExpiryDate.AddDays(1); // Service might adjust the date
        var command = new UpgradeTenantCommand(tenantId, extendedExpiryDate);

        _tenantService.UpgradeSubscriptionAsync(tenantId, extendedExpiryDate, Arg.Any<CancellationToken>())
            .Returns(returnedValidity);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Tenant.ShouldBe(tenantId);
        result.NewValidity.ShouldBe(returnedValidity);
    }

    [Fact]
    public async Task Handle_Should_ThrowArgumentNullException_When_CommandIsNull()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_PassCancellationToken_ToService()
    {
        // Arrange
        var tenantId = "tenant-1";
        var extendedExpiryDate = DateTime.UtcNow.AddYears(1);
        var command = new UpgradeTenantCommand(tenantId, extendedExpiryDate);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _tenantService.UpgradeSubscriptionAsync(tenantId, extendedExpiryDate, token)
            .Returns(extendedExpiryDate);

        // Act
        await _sut.Handle(command, token);

        // Assert
        await _tenantService.Received(1).UpgradeSubscriptionAsync(tenantId, extendedExpiryDate, token);
    }

    #endregion

    #region Date Handling Tests

    [Fact]
    public async Task Handle_Should_WorkWithPastDate()
    {
        // Arrange
        var tenantId = "tenant-1";
        var pastDate = DateTime.UtcNow.AddDays(-30);
        var command = new UpgradeTenantCommand(tenantId, pastDate);

        _tenantService.UpgradeSubscriptionAsync(tenantId, pastDate, Arg.Any<CancellationToken>())
            .Returns(pastDate);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.NewValidity.ShouldBe(pastDate);
    }

    [Fact]
    public async Task Handle_Should_WorkWithFarFutureDate()
    {
        // Arrange
        var tenantId = "tenant-1";
        var futureDate = DateTime.UtcNow.AddYears(10);
        var command = new UpgradeTenantCommand(tenantId, futureDate);

        _tenantService.UpgradeSubscriptionAsync(tenantId, futureDate, Arg.Any<CancellationToken>())
            .Returns(futureDate);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.NewValidity.ShouldBe(futureDate);
    }

    #endregion
}