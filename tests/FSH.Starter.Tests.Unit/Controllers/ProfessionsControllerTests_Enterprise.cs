using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FSH.Starter.WebApi.Controllers;
using FSH.Framework.Core.Common.Interfaces;
using FSH.Starter.WebApi.Contracts.Common;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace FSH.Starter.Tests.Unit.Controllers
{
    public class ProfessionsControllerTests_Enterprise
    {
        [Fact]
        public async Task GetAllProfessionsAsync_RepositoryThrows_ReturnsFailureAsync()
        {
            var mockRepo = new Mock<IProfessionRepository>();
            mockRepo.Setup(r => r.GetAllActiveProfessionsAsync()).ThrowsAsync(new Exception("db error"));
            var controller = new ProfessionsController(mockRepo.Object);

            var result = await controller.GetAllProfessionsAsync();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<IReadOnlyList<ProfessionDto>>>(okResult.Value);
            Assert.False(response.Success);
            Assert.Contains("db error", response.Message, StringComparison.InvariantCultureIgnoreCase);
        }

        [Fact]
        public async Task GetProfessionByIdAsync_RepositoryThrows_ReturnsFailureAsync()
        {
            var mockRepo = new Mock<IProfessionRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));
            var controller = new ProfessionsController(mockRepo.Object);

            var result = await controller.GetProfessionByIdAsync(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProfessionDto>>(okResult.Value);
            Assert.False(response.Success);
            Assert.Contains("db error", response.Message, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
