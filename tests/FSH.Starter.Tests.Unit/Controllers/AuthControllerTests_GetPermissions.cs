using FSH.Starter.WebApi.Host;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace FSH.Starter.Tests.Unit.Controllers;

public class AuthControllerTests_GetPermissions
{
    private readonly IMediator _mediator;
    private readonly AuthController _controller;

    public AuthControllerTests_GetPermissions()
    {
        _mediator = Substitute.For<IMediator>();
        _controller = new AuthController(_mediator);
    }

    [Fact]
    public void GetPermissions_WithUserRoles_ShouldReturnRolesList()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "admin"),
            new Claim(ClaimTypes.Role, "user"),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;
        
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = httpContext
        };

        // Act
        var result = _controller.GetPermissions();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var roles = Assert.IsType<List<string>>(okResult.Value);
        Assert.Contains("admin", roles);
        Assert.Contains("user", roles);
        Assert.Equal(2, roles.Count);
    }

    [Fact]
    public void GetPermissions_WithNoRoles_ShouldReturnEmptyList()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;
        
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = httpContext
        };

        // Act
        var result = _controller.GetPermissions();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var roles = Assert.IsType<List<string>>(okResult.Value);
        Assert.Empty(roles);
    }
}
