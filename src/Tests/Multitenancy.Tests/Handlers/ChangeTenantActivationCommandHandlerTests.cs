using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using FSH.Modules.Multitenancy.Contracts.v1.ChangeTenantActivation;
using FSH.Modules.Multitenancy.Features.v1.ChangeTenantActivation;
using NSubstitute;

namespace Multitenancy.Tests.Handlers;

/// <summary>
/// Tests for ChangeTenantActivationCommandHandler - handles tenant activation/deactivation.
/// </summary>
public sealed class ChangeTenantActivationCommandHandlerTests
{
    private readonly ITenantService _tenantService;
    private readonly ChangeTenantActivationCommandHandler _sut;

    public ChangeTenantActivationCommandHandlerTests()
    {
        _tenantService = Substitute.For<ITenantService>();
        _sut = new ChangeTenantActivationCommandHandler(_tenantService);
    }

    #region Handle - Activation Tests

    [Fact]
    public async Task Handle_Should_CallActivateAsync_When_IsActiveIsTrue()
    {
        // Arrange
        var tenantId = "tenant-1";
        var command = new ChangeTenantActivationCommand(tenantId, true);
        var expectedMessage = $"tenant {tenantId} is now activated";
        var expectedStatus = new TenantStatusDto
        {
            Id = tenantId,
            Name = "Test Tenant",
            IsActive = true,
            ValidUpto = DateTime.UtcNow.AddYears(1),
            AdminEmail = "admin@test.com"
        };

        _tenantService.ActivateAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(expectedMessage);
        _tenantService.GetStatusAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(expectedStatus);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _tenantService.Received(1).ActivateAsync(tenantId, Arg.Any<CancellationToken>());
        await _tenantService.DidNotReceive().DeactivateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        result.TenantId.ShouldBe(tenantId);
        result.IsActive.ShouldBeTrue();
        result.Message.ShouldBe(expectedMessage);
    }

    [Fact]
    public async Task Handle_Should_ReturnCorrectResult_When_Activating()
    {
        // Arrange
        var tenantId = "tenant-1";
        var validUpto = DateTime.UtcNow.AddYears(1);
        var command = new ChangeTenantActivationCommand(tenantId, true);

        _tenantService.ActivateAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns("activated");
        _tenantService.GetStatusAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new TenantStatusDto
            {
                Id = tenantId,
                Name = "Test Tenant",
                IsActive = true,
                ValidUpto = validUpto,
                AdminEmail = "admin@test.com"
            });

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.TenantId.ShouldBe(tenantId);
        result.IsActive.ShouldBeTrue();
        result.ValidUpto.ShouldBe(validUpto);
    }

    #endregion

    #region Handle - Deactivation Tests

    [Fact]
    public async Task Handle_Should_CallDeactivateAsync_When_IsActiveIsFalse()
    {
        // Arrange
        var tenantId = "tenant-1";
        var command = new ChangeTenantActivationCommand(tenantId, false);
        var expectedMessage = $"tenant {tenantId} is now deactivated";
        var expectedStatus = new TenantStatusDto
        {
            Id = tenantId,
            Name = "Test Tenant",
            IsActive = false,
            ValidUpto = DateTime.MinValue,
            AdminEmail = "admin@test.com"
        };

        _tenantService.DeactivateAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(expectedMessage);
        _tenantService.GetStatusAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(expectedStatus);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _tenantService.Received(1).DeactivateAsync(tenantId, Arg.Any<CancellationToken>());
        await _tenantService.DidNotReceive().ActivateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        result.TenantId.ShouldBe(tenantId);
        result.IsActive.ShouldBeFalse();
        result.Message.ShouldBe(expectedMessage);
    }

    [Fact]
    public async Task Handle_Should_ReturnCorrectResult_When_Deactivating()
    {
        // Arrange
        var tenantId = "tenant-1";
        var command = new ChangeTenantActivationCommand(tenantId, false);

        _tenantService.DeactivateAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns("deactivated");
        _tenantService.GetStatusAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new TenantStatusDto
            {
                Id = tenantId,
                Name = "Test Tenant",
                IsActive = false,
                ValidUpto = DateTime.MinValue,
                AdminEmail = "admin@test.com"
            });

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.TenantId.ShouldBe(tenantId);
        result.IsActive.ShouldBeFalse();
    }

    #endregion

    #region Handle - Null Command Tests

    [Fact]
    public async Task Handle_Should_ThrowArgumentNullException_When_CommandIsNull()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _sut.Handle(null!, CancellationToken.None));
    }

    #endregion

    #region Handle - CancellationToken Tests

    [Fact]
    public async Task Handle_Should_PassCancellationToken_ToActivateAsync()
    {
        // Arrange
        var tenantId = "tenant-1";
        var command = new ChangeTenantActivationCommand(tenantId, true);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _tenantService.ActivateAsync(tenantId, token)
            .Returns("activated");
        _tenantService.GetStatusAsync(tenantId, token)
            .Returns(new TenantStatusDto
            {
                Id = tenantId,
                Name = "Test",
                IsActive = true,
                AdminEmail = "admin@test.com"
            });

        // Act
        await _sut.Handle(command, token);

        // Assert
        await _tenantService.Received(1).ActivateAsync(tenantId, token);
        await _tenantService.Received(1).GetStatusAsync(tenantId, token);
    }

    [Fact]
    public async Task Handle_Should_PassCancellationToken_ToDeactivateAsync()
    {
        // Arrange
        var tenantId = "tenant-1";
        var command = new ChangeTenantActivationCommand(tenantId, false);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _tenantService.DeactivateAsync(tenantId, token)
            .Returns("deactivated");
        _tenantService.GetStatusAsync(tenantId, token)
            .Returns(new TenantStatusDto
            {
                Id = tenantId,
                Name = "Test",
                IsActive = false,
                AdminEmail = "admin@test.com"
            });

        // Act
        await _sut.Handle(command, token);

        // Assert
        await _tenantService.Received(1).DeactivateAsync(tenantId, token);
        await _tenantService.Received(1).GetStatusAsync(tenantId, token);
    }

    #endregion
}