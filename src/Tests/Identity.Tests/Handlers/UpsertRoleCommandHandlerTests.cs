using AutoFixture;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Roles.UpsertRole;
using FSH.Modules.Identity.Features.v1.Roles.UpsertRole;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Identity.Tests.Handlers;

public sealed class UpsertRoleCommandHandlerTests
{
    private readonly IRoleService _roleService;
    private readonly UpsertRoleCommandHandler _sut;
    private readonly IFixture _fixture;

    public UpsertRoleCommandHandlerTests()
    {
        _roleService = Substitute.For<IRoleService>();
        _sut = new UpsertRoleCommandHandler(_roleService);
        _fixture = new Fixture();
    }

    [Fact]
    public async Task Handle_Should_CallCreateOrUpdateRoleAsync_WithCorrectParameters()
    {
        // Arrange
        var command = _fixture.Create<UpsertRoleCommand>();
        var expectedDto = _fixture.Create<RoleDto>();
        _roleService.CreateOrUpdateRoleAsync(command.Id, command.Name, command.Description ?? string.Empty, Arg.Any<CancellationToken>())
            .Returns(expectedDto);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBe(expectedDto);
        await _roleService.Received(1).CreateOrUpdateRoleAsync(command.Id, command.Name, command.Description ?? string.Empty, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_HandleNullDescription_WithEmptyString()
    {
        // Arrange
        var command = new UpsertRoleCommand
        {
            Id = _fixture.Create<string>(),
            Name = "Admin",
            Description = null
        };
        var expectedDto = _fixture.Create<RoleDto>();
        _roleService.CreateOrUpdateRoleAsync(command.Id, "Admin", string.Empty, Arg.Any<CancellationToken>())
            .Returns(expectedDto);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _roleService.Received(1).CreateOrUpdateRoleAsync(command.Id, "Admin", string.Empty, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ThrowArgumentNullException_When_CommandIsNull()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_PassCancellationToken_ToRoleService()
    {
        // Arrange
        var command = _fixture.Create<UpsertRoleCommand>();
        using var cts = new CancellationTokenSource();

        // Act
        await _sut.Handle(command, cts.Token);

        // Assert
        await _roleService.Received(1).CreateOrUpdateRoleAsync(command.Id, command.Name, command.Description ?? string.Empty, cts.Token);
    }
}
