using FSH.Starter.WebApi.Controllers;
using FSH.Framework.Core.Common.Interfaces;
using Moq;
using Xunit;

namespace FSH.Starter.Tests.Unit.Controllers;

public class SimpleConstructorTests
{
    [Fact]
    public void ProfessionsController_Constructor_ShouldSetRepository()
    {
        // Arrange
        var mockRepo = new Mock<IProfessionRepository>();

        // Act
        var controller = new ProfessionsController(mockRepo.Object);

        // Assert
        Assert.NotNull(controller);
    }
}
