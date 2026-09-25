using System.Net;
using FSH.Framework.Web.Origin;
using FSH.Modules.Identity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Identity.Tests.Services;

/// <summary>
/// Tests for RequestContextService - exposes HTTP request metadata through an abstraction.
/// </summary>
public sealed class RequestContextServiceTests
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequestContextServiceTests()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
    }

    private RequestContextService CreateService(Uri? originUrl = null)
    {
        var originOptions = Options.Create(new OriginOptions { OriginUrl = originUrl });
        return new RequestContextService(_httpContextAccessor, originOptions);
    }

    private void SetHttpContext(HttpContext? context)
    {
        _httpContextAccessor.HttpContext.Returns(context);
    }

    #region IpAddress Tests

    [Fact]
    public void IpAddress_Should_ReturnRemoteIp_When_ConnectionHasAddress()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.5");
        SetHttpContext(context);
        var service = CreateService();

        // Act
        var result = service.IpAddress;

        // Assert
        result.ShouldBe("203.0.113.5");
    }

    [Fact]
    public void IpAddress_Should_ReturnNull_When_NoHttpContext()
    {
        // Arrange
        SetHttpContext(null);
        var service = CreateService();

        // Act
        var result = service.IpAddress;

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region UserAgent Tests

    [Fact]
    public void UserAgent_Should_ReturnHeaderValue_When_Present()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers.UserAgent = "Mozilla/5.0 TestAgent";
        SetHttpContext(context);
        var service = CreateService();

        // Act
        var result = service.UserAgent;

        // Assert
        result.ShouldBe("Mozilla/5.0 TestAgent");
    }

    [Fact]
    public void UserAgent_Should_ReturnEmptyString_When_HeaderMissing()
    {
        // Arrange - StringValues.ToString() on a missing header yields an empty string
        var context = new DefaultHttpContext();
        SetHttpContext(context);
        var service = CreateService();

        // Act
        var result = service.UserAgent;

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void UserAgent_Should_ReturnNull_When_NoHttpContext()
    {
        // Arrange
        SetHttpContext(null);
        var service = CreateService();

        // Act
        var result = service.UserAgent;

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region ClientId Tests

    [Fact]
    public void ClientId_Should_ReturnHeaderValue_When_XClientIdPresent()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Client-Id"] = "mobile-app";
        SetHttpContext(context);
        var service = CreateService();

        // Act
        var result = service.ClientId;

        // Assert
        result.ShouldBe("mobile-app");
    }

    [Fact]
    public void ClientId_Should_ReturnWebDefault_When_HeaderMissing()
    {
        // Arrange
        var context = new DefaultHttpContext();
        SetHttpContext(context);
        var service = CreateService();

        // Act
        var result = service.ClientId;

        // Assert
        result.ShouldBe("web");
    }

    [Fact]
    public void ClientId_Should_ReturnWebDefault_When_HeaderWhitespace()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Client-Id"] = "   ";
        SetHttpContext(context);
        var service = CreateService();

        // Act
        var result = service.ClientId;

        // Assert
        result.ShouldBe("web");
    }

    [Fact]
    public void ClientId_Should_ReturnWebDefault_When_NoHttpContext()
    {
        // Arrange
        SetHttpContext(null);
        var service = CreateService();

        // Act
        var result = service.ClientId;

        // Assert
        result.ShouldBe("web");
    }

    #endregion

    #region Origin Tests

    [Fact]
    public void Origin_Should_ReturnConfiguredOrigin_When_OriginUrlConfigured()
    {
        // Arrange - configured origin wins over request-derived origin and is trailing-slash trimmed
        var service = CreateService(new Uri("https://configured.example.com/"));
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("request.example.com");
        SetHttpContext(context);

        // Act
        var result = service.Origin;

        // Assert
        result.ShouldBe("https://configured.example.com");
    }

    [Fact]
    public void Origin_Should_DeriveFromRequest_When_NoOriginConfigured()
    {
        // Arrange
        var service = CreateService(originUrl: null);
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("app.example.com");
        SetHttpContext(context);

        // Act
        var result = service.Origin;

        // Assert
        result.ShouldBe("https://app.example.com");
    }

    [Fact]
    public void Origin_Should_IncludePathBase_When_RequestHasPathBase()
    {
        // Arrange
        var service = CreateService(originUrl: null);
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("app.example.com");
        context.Request.PathBase = new PathString("/api");
        SetHttpContext(context);

        // Act
        var result = service.Origin;

        // Assert
        result.ShouldBe("https://app.example.com/api");
    }

    [Fact]
    public void Origin_Should_ReturnNull_When_NoOriginConfiguredAndNoHttpContext()
    {
        // Arrange
        var service = CreateService(originUrl: null);
        SetHttpContext(null);

        // Act
        var result = service.Origin;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Origin_Should_ReturnNull_When_RequestHasNoHost()
    {
        // Arrange
        var service = CreateService(originUrl: null);
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        // Host left unset (HostString has no value)
        SetHttpContext(context);

        // Act
        var result = service.Origin;

        // Assert
        result.ShouldBeNull();
    }

    #endregion
}
