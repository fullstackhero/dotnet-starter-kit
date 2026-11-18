using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Identity.Domain;
using FSH.Modules.Identity.Features.v1.Roles;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace Identity.Tests.Services;

/// <summary>
/// RoleManager reports a failed create/update/delete (duplicate name, concurrency, store error)
/// through <see cref="IdentityResult.Succeeded"/>, not by throwing. RoleService must surface that
/// failure instead of answering with a role that was never written.
/// </summary>
public sealed class RoleServiceIdentityResultTests
{
    private static readonly IdentityError DuplicateName = new()
    {
        Code = nameof(IdentityErrorDescriber.DuplicateRoleName),
        Description = "Role name 'Managers' is already taken.",
    };

    private readonly RoleManager<FshRole> _roleManager = Substitute.For<RoleManager<FshRole>>(
        Substitute.For<IRoleStore<FshRole>>(), null, null, null, null);

    private RoleService CreateSut() => new(_roleManager, null!, null!, null!, null!);

    [Fact]
    public async Task CreateOrUpdateRoleAsync_Should_Throw_When_CreateFails()
    {
        _roleManager.CreateAsync(Arg.Any<FshRole>()).Returns(IdentityResult.Failed(DuplicateName));

        var ex = await Should.ThrowAsync<CustomException>(
            () => CreateSut().CreateOrUpdateRoleAsync(string.Empty, "Managers", "duplicate"));

        ex.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ex.ErrorMessages.ShouldContain(DuplicateName.Description);
    }

    [Fact]
    public async Task CreateOrUpdateRoleAsync_Should_Throw_When_UpdateFails()
    {
        var role = new FshRole("Sales", "existing role");
        _roleManager.FindByIdAsync(role.Id).Returns(role);
        _roleManager.UpdateAsync(role).Returns(IdentityResult.Failed(DuplicateName));

        var ex = await Should.ThrowAsync<CustomException>(
            () => CreateSut().CreateOrUpdateRoleAsync(role.Id, "Managers", "rename onto a taken name"));

        ex.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ex.ErrorMessages.ShouldContain(DuplicateName.Description);
    }

    [Fact]
    public async Task CreateOrUpdateRoleAsync_Should_ReturnRole_When_CreateSucceeds()
    {
        _roleManager.CreateAsync(Arg.Any<FshRole>()).Returns(IdentityResult.Success);

        var dto = await CreateSut().CreateOrUpdateRoleAsync(string.Empty, "Managers", "new role");

        dto.Name.ShouldBe("Managers");
        dto.Id.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task DeleteRoleAsync_Should_Throw_When_DeleteFails()
    {
        var role = new FshRole("Sales", "existing role");
        var concurrency = new IdentityError
        {
            Code = nameof(IdentityErrorDescriber.ConcurrencyFailure),
            Description = "Optimistic concurrency failure, object has been modified.",
        };
        // First lookup is the delete's own; the second (affected-user cache invalidation) finds no
        // role and returns early, which keeps the DbContext out of this unit test.
        _roleManager.FindByIdAsync(role.Id).Returns(role, (FshRole?)null);
        _roleManager.DeleteAsync(role).Returns(IdentityResult.Failed(concurrency));

        var ex = await Should.ThrowAsync<CustomException>(() => CreateSut().DeleteRoleAsync(role.Id));

        ex.ErrorMessages.ShouldContain(concurrency.Description);
    }
}
