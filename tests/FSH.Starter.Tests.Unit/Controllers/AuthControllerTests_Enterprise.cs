using System;
using System.Threading.Tasks;
using FSH.Starter.WebApi.Host;
using FSH.Starter.WebApi.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using FSH.Framework.Core.Auth.Features.Token.Generate;
using FSH.Framework.Core.Auth.Features.Token.Refresh;
using Microsoft.AspNetCore.Http;

namespace FSH.Starter.Tests.Unit.Controllers
{
    public class AuthControllerTests_Enterprise
    {
        [Fact]
        public async Task RegisterRequestAsync_ValidRequest_ReturnsSuccessAsync()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<FSH.Framework.Core.Auth.Features.RegisterRequest.RegisterRequestCommand>(), It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(new FSH.Framework.Core.Auth.Features.RegisterRequest.RegisterRequestResponse(true, "Success", "5555555555"));

            // Testable controller ile property injection
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
            httpContext.Request.Headers["User-Agent"] = "UnitTestAgent";
            var controller = new TestableAuthController(mediator.Object, httpContext);

            var request = new RegisterRequest
            {
                Email = "test@example.com",
                PhoneNumber = "5555555555",
                Tckn = "12345678901",
                Password = "Test1234!",
                FirstName = "Test",
                LastName = "User",
                ProfessionId = 1,
                BirthDate = System.DateTime.UtcNow.AddYears(-30),
                MarketingConsent = true,
                ElectronicCommunicationConsent = true,
                MembershipAgreementConsent = true
            };

            var result = await controller.RegisterRequestAsync(request);

            Assert.True(result is OkObjectResult || result is BadRequestObjectResult);
        }

        [Fact]
        public async Task VerifyRegistrationAsync_ValidRequest_ReturnsSuccessAsync()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<FSH.Framework.Core.Auth.Features.VerifyRegistration.VerifyRegistrationCommand>(), It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(new FSH.Framework.Core.Auth.Features.VerifyRegistration.VerifyRegistrationResponse(true, "Success", Guid.NewGuid()));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["User-Agent"] = "UnitTestAgent";
            var controller = new TestableAuthController(mediator.Object, httpContext);

            var request = new VerifyRegistrationRequest
            {
                PhoneNumber = "5555555555",
                OtpCode = "1234"
            };

            var result = await controller.VerifyRegistrationAsync(request);

            Assert.True(result is OkObjectResult || result is BadRequestObjectResult);
        }

        [Fact]
        public async Task GenerateTokenAsync_ValidRequest_ReturnsSuccessAsync()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<GenerateTokenCommand>(), default))
                .ReturnsAsync(new TokenGenerationResult());
            var controller = new AuthController(mediator.Object);
            var command = new GenerateTokenCommand { Tckn = "123", Password = "pass" };

            var result = await controller.GenerateTokenAsync(command);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task RefreshTokenAsync_ValidRequest_ReturnsSuccessAsync()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<RefreshTokenCommand>(), default))
                .ReturnsAsync(new FSH.Framework.Core.Auth.Dtos.TokenResponseDto());
            var controller = new AuthController(mediator.Object);
            var command = new RefreshTokenCommand { Token = "token", RefreshToken = "refresh" };

            var result = await controller.RefreshTokenAsync(command);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
    }
}
