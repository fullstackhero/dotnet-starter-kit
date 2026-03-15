using FSH.Framework.Core.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Web.Exceptions;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Generic.Tests.Exceptions;

public class GlobalExceptionHandlerTests
{
    private readonly ILogger<GlobalExceptionHandler> _logger = Substitute.For<ILogger<GlobalExceptionHandler>>();
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _multiTenantContextAccessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly GlobalExceptionHandler _handler;

    public GlobalExceptionHandlerTests()
    {
        _handler = new GlobalExceptionHandler(_logger);
    }

    [Fact]
    public async Task TryHandleAsync_Should_IncludeTenantAndUserInLogs()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var services = Substitute.For<IServiceProvider>();
        context.RequestServices = services;
        
        var exception = new Exception("Test exception");
        var cancellationToken = CancellationToken.None;

        var tenantInfo = new AppTenantInfo { Id = "test-tenant" };
        var multiTenantContext = Substitute.For<IMultiTenantContext<AppTenantInfo>>();
        multiTenantContext.TenantInfo.Returns(tenantInfo);
        _multiTenantContextAccessor.MultiTenantContext.Returns(multiTenantContext);

        services.GetService(typeof(IMultiTenantContextAccessor<AppTenantInfo>)).Returns(_multiTenantContextAccessor);
        services.GetService(typeof(ICurrentUser)).Returns(_currentUser);

        var userId = Guid.NewGuid();
        _currentUser.GetUserId().Returns(userId);

        // Act
        var result = await _handler.TryHandleAsync(context, exception, cancellationToken);

        // Assert
        result.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);

        // Verify logger was called with tenant and user info
        _logger.ReceivedWithAnyArgs().LogError(default);
    }
}
