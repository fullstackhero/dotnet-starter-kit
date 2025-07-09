using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FSH.Starter.WebApi.Controllers;
using FSH.Framework.Core.Common.Interfaces;
using FSH.Starter.WebApi.Contracts.Common;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace FSH.Starter.Tests.Unit.Controllers
{
    public class ProfessionsControllerTests
    {
        [Fact]
        public async Task GetAllProfessionsAsync_ReturnsSuccessAsync()
        {
            // Arrange
            var mockRepo = new Mock<IProfessionRepository>();
            mockRepo.Setup(r => r.GetAllActiveProfessionsAsync())
                .ReturnsAsync(new List<ProfessionDto> { new ProfessionDto() });
            var controller = new ProfessionsController(mockRepo.Object);

            // Act
            var result = await controller.GetAllProfessionsAsync();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<IReadOnlyList<ProfessionDto>>>(okResult.Value);
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
        }

        [Fact]
        public async Task GetProfessionByIdAsync_ReturnsNotFound_WhenNullAsync()
        {
            // Arrange
            var mockRepo = new Mock<IProfessionRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ProfessionDto)null!);
            var controller = new ProfessionsController(mockRepo.Object);

            // Act
            var result = await controller.GetProfessionByIdAsync(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProfessionDto>>(okResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Profession not found", response.Message);
        }

        [Fact]
        public async Task GetProfessionByIdAsync_ReturnsSuccess_WhenFoundAsync()
        {
            // Arrange
            var profession = new ProfessionDto { Id = 1, Name = "Software Engineer" };
            var mockRepo = new Mock<IProfessionRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(profession);
            var controller = new ProfessionsController(mockRepo.Object);

            // Act
            var result = await controller.GetProfessionByIdAsync(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProfessionDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal(profession, response.Data);
        }

        [Fact]
        public async Task GetProfessionByIdAsync_ReturnsSuccess_WithCorrectProfessionAsync()
        {
            // Arrange
            var expectedProfession = new ProfessionDto { Id = 2, Name = "Data Scientist" };
            var mockRepo = new Mock<IProfessionRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(expectedProfession);
            var controller = new ProfessionsController(mockRepo.Object);

            // Act
            var result = await controller.GetProfessionByIdAsync(2);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProfessionDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(expectedProfession.Id, response.Data!.Id);
            Assert.Equal(expectedProfession.Name, response.Data.Name);
        }

        [Fact]
        public async Task GetAllProfessionsAsync_ReturnsFailure_WhenExceptionThrownAsync()
        {
            // Arrange
            var mockRepo = new Mock<IProfessionRepository>();
            mockRepo.Setup(r => r.GetAllActiveProfessionsAsync())
                .ThrowsAsync(new System.Exception("Database error"));
            var controller = new ProfessionsController(mockRepo.Object);

            // Act
            var result = await controller.GetAllProfessionsAsync();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<IReadOnlyList<ProfessionDto>>>(okResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Error getting professions", response.Message, StringComparison.Ordinal);
        }
    }
}
