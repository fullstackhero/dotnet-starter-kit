using System.Linq.Expressions;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Jobs.Services;
using FSH.Framework.Mailing;
using FSH.Framework.Mailing.Services;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Domain;
using FSH.Modules.Identity.Services;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace Identity.Tests.Services;

/// <summary>
/// Tests for UserPasswordService.ForgotPasswordAsync — focuses on the reset-link format
/// (regression cover for the double-slash, missing-tenant and unencoded-email defects).
/// </summary>
public sealed class UserPasswordServiceTests
{
    private const string TenantId = "codefi";

    private readonly UserManager<FshUser> _userManager;
    private readonly IJobService _jobService;
    private readonly IMailService _mailService;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantAccessor;

    public UserPasswordServiceTests()
    {
        _userManager = Substitute.For<UserManager<FshUser>>(
            Substitute.For<IUserStore<FshUser>>(), null, null, null, null, null, null, null, null);
        _jobService = Substitute.For<IJobService>();
        _mailService = Substitute.For<IMailService>();
        _tenantAccessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();

        var mtContext = Substitute.For<IMultiTenantContext<AppTenantInfo>>();
        mtContext.TenantInfo.Returns(new AppTenantInfo(TenantId, TenantId, "Codefi"));
        _tenantAccessor.MultiTenantContext.Returns(mtContext);

        // The mail job is enqueued as an expression; compile + invoke it so the captured MailRequest
        // reaches the (mocked) mail service exactly as production would build it.
        _jobService.Enqueue(Arg.Any<Expression<Func<Task>>>())
            .Returns(ci =>
            {
                ci.Arg<Expression<Func<Task>>>().Compile().Invoke();
                return "job-1";
            });
        _mailService.SendAsync(Arg.Any<MailRequest>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
    }

    private UserPasswordService CreateSut() =>
        new(_userManager, null!, _jobService, _mailService, _tenantAccessor, null!, null!);

    private MailRequest CaptureSentMail()
    {
        var call = _mailService.ReceivedCalls().Single();
        return (MailRequest)call.GetArguments()[0]!;
    }

    [Fact]
    public async Task ForgotPasswordAsync_Should_BuildResetLink_WithSingleSlash_Tenant_AndEncodedEmail()
    {
        // Arrange — trailing slash on the origin (as Uri.ToString() produces for a host-only URL) and an
        // email with reserved characters ('+', '@') to exercise all three defects at once.
        const string email = "marcelo+reset@codefi.com.br";
        var user = new FshUser { Email = email, UserName = email };
        _userManager.FindByEmailAsync(email).Returns(user);
        _userManager.GeneratePasswordResetTokenAsync(user).Returns("raw-token");

        var sut = CreateSut();

        // Act
        await sut.ForgotPasswordAsync(email, "https://appbase.codefi.com.br/", CancellationToken.None);

        // Assert
        var body = CaptureSentMail().Body!;
        body.ShouldContain("https://appbase.codefi.com.br/reset-password?");
        body.ShouldNotContain("//reset-password");                       // defect 3: no double slash
        body.ShouldContain($"&tenant={TenantId}");                       // defect 4: tenant present
        // defect 5: reserved chars are encoded — '+' must become %2B (an unencoded '+' would decode to a
        // space). '@' is left as-is, which is valid in a query component per RFC 3986 (QueryHelpers encodes
        // only what is required, matching GetEmailVerificationUriAsync).
        body.ShouldContain("email=marcelo%2Breset");
        body.ShouldNotContain("email=marcelo+reset");                    // raw '+' must not leak
    }

    [Fact]
    public async Task ForgotPasswordAsync_Should_NotEnqueueMail_When_UserIsUnknown()
    {
        // Arrange — anti-enumeration: unknown user silently no-ops (no mail), still a 200 upstream.
        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns((FshUser?)null);
        var sut = CreateSut();

        // Act
        await sut.ForgotPasswordAsync("ghost@codefi.com.br", "https://appbase.codefi.com.br/", CancellationToken.None);

        // Assert
        _jobService.DidNotReceive().Enqueue(Arg.Any<Expression<Func<Task>>>());
    }
}
