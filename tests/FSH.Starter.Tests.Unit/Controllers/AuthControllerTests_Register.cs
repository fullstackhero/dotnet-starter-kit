using System;
using System.Threading.Tasks;
using FSH.Framework.Core.Auth.Features.RegisterRequest;
using FSH.Starter.WebApi.Contracts.Auth;
using FSH.Starter.WebApi.Contracts.Common;
using FSH.Starter.WebApi.Host;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace FSH.Starter.Tests.Unit.Controllers
{
    public class AuthControllerTests_Register
    {
        private readonly Mock<IMediator> _mediator;
        private readonly AuthController _controller;

        public AuthControllerTests_Register()
        {
            _mediator = new Mock<IMediator>();
            _controller = new AuthController(_mediator.Object);

            // Mock HttpContext
            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = httpContext,
            };
        }

        [Fact]
        public async Task RegisterRequestAsync_ValidRequest_ReturnsSuccessAsync()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "test@example.com",
                PhoneNumber = "5551234567",
                Tckn = "10000000146",
                Password = "ValidPassword123!",
                FirstName = "Test",
                LastName = "User",
                ProfessionId = 1,
                BirthDate = new DateTime(1990, 1, 1),
                MarketingConsent = true,
                ElectronicCommunicationConsent = true,
                MembershipAgreementConsent = true
            };

            var successResponse = new RegisterRequestResponse(true, "Operation successful");

            _mediator.Setup(m => m.Send(It.IsAny<RegisterRequestCommand>(), default))
                .ReturnsAsync(successResponse);

            // Act
            var result = await _controller.RegisterRequestAsync(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<RegisterRequestResponse>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal("Operation successful", apiResponse.Message);
        }

        [Fact]
        public async Task RegisterRequestAsync_MediatorReturnsFailure_ReturnsBadRequestAsync()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "test@example.com",
                PhoneNumber = "5551234567",
                Tckn = "10000000146",
                Password = "ValidPassword123!",
                FirstName = "Test",
                LastName = "User",
                ProfessionId = 1,
                BirthDate = new DateTime(1990, 1, 1),
                MarketingConsent = true,
                ElectronicCommunicationConsent = true,
                MembershipAgreementConsent = true
            };

            var failureResponse = new RegisterRequestResponse(false, "Error");

            _mediator.Setup(m => m.Send(It.IsAny<RegisterRequestCommand>(), default))
                .ReturnsAsync(failureResponse);

            // Act
            var result = await _controller.RegisterRequestAsync(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<RegisterRequestResponse>>(badRequestResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Equal("Error", apiResponse.Message);
        }
    }
}
