using System.Security.Claims;
using FSH.Framework.Core.Context;
using FSH.Framework.Shared.Constants;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Impersonation;
using FSH.Modules.Identity.Contracts.v1.Impersonation.StartImpersonation;
using FSH.Modules.Identity.Features.v1.Impersonation.StartImpersonation;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.IdentityModel.Tokens.Jwt;

namespace Identity.Tests.Handlers;

/// <summary>
/// The impersonation token must NOT carry the target user's `locale` claim — language is a
/// presentation concern, so the operator keeps reading in their own language.
/// </summary>
public sealed class StartImpersonationCommandHandlerTests
{
    private const string TenantId = "codefi";
    private const string TargetUserId = "target-user";

    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly ISecurityAudit _securityAudit = Substitute.For<ISecurityAudit>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly IImpersonationGrantService _grantService = Substitute.For<IImpersonationGrantService>();

    private StartImpersonationCommandHandler CreateSut() =>
        new(_identityService, _tokenService, _securityAudit, _currentUser, _requestContext,
            _grantService, TimeProvider.System, Substitute.For<ILogger<StartImpersonationCommandHandler>>());

    [Fact]
    public async Task Handle_strips_locale_but_preserves_identity_claims_and_injects_actor()
    {
        // Arrange — an authenticated operator in the same tenant as the target.
        var actorUserId = Guid.NewGuid();
        _currentUser.IsAuthenticated().Returns(true);
        _currentUser.GetUserId().Returns(actorUserId);
        _currentUser.GetTenant().Returns(TenantId);
        _currentUser.Name.Returns("operator");
        _currentUser.GetUserClaims().Returns(new List<Claim>());

        // A realistic target claim set: the persisted `locale` must be dropped, but every other
        // identity claim (name, role, subject, tenant) must survive into the impersonation token.
        var targetClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, "orig-jti"),
            new(JwtRegisteredClaimNames.Sub, TargetUserId),
            new(ClaimConstants.Tenant, TenantId),
            new(ClaimTypes.Name, "Target User"),
            new(ClaimTypes.Role, "Admin"),
            new("locale", "pt-BR"),
        };
        _identityService
            .BuildClaimsForUserAsync(TargetUserId, TenantId, Arg.Any<CancellationToken>())
            .Returns(((string, IEnumerable<Claim>)?)(TargetUserId, targetClaims));

        IEnumerable<Claim>? issuedClaims = null;
        _tokenService
            .IssueAccessOnlyAsync(
                Arg.Any<string>(),
                Arg.Do<IEnumerable<Claim>>(c => issuedClaims = c),
                Arg.Any<TimeSpan?>(),
                Arg.Any<CancellationToken>())
            .Returns(("access-token", DateTime.UtcNow.AddMinutes(15)));

        var sut = CreateSut();

        // Act
        await sut.Handle(new StartImpersonationCommand(TargetUserId, TenantId, "reason", 15), CancellationToken.None);

        // Assert
        issuedClaims.ShouldNotBeNull();
        var issued = issuedClaims.ToList();

        // (a) locale is stripped — the operator keeps reading in their own language.
        issued.ShouldNotContain(c => c.Type == "locale");

        // (b) every NON-locale identity claim survives — a mutation that over-strips (e.g. drops Name,
        //     role, sub or tenant) must fail here.
        issued.ShouldContain(c => c.Type == ClaimTypes.Name && c.Value == "Target User");
        issued.ShouldContain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        issued.ShouldContain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == TargetUserId);
        issued.ShouldContain(c => c.Type == ClaimConstants.Tenant && c.Value == TenantId);

        // (c) RFC 8693 actor claims are injected so the token records who is acting.
        issued.ShouldContain(c => c.Type == ClaimConstants.ActorSubject && c.Value == actorUserId.ToString());
        issued.ShouldContain(c => c.Type == ClaimConstants.ActorTenant && c.Value == TenantId);

        // (d) the jti is swapped: the target's original jti is gone and exactly one fresh, non-empty
        //     jti is present (so the persisted grant row and the JWT share a new identifier).
        issued.ShouldNotContain(c => c.Type == JwtRegisteredClaimNames.Jti && c.Value == "orig-jti");
        var jti = issued.Single(c => c.Type == JwtRegisteredClaimNames.Jti);
        jti.Value.ShouldNotBe("orig-jti");
        jti.Value.ShouldNotBeNullOrWhiteSpace();
    }
}
