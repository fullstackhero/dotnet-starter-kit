using FSH.Starter.WebApi.Contracts.Common;
using Xunit;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FSH.Starter.Tests.Unit.Contracts.Common;

public class ApiResponseTests
{
    [Fact]
    public void ApiResponse_SuccessResult_ShouldCreateSuccessResponse()
    {
        // Arrange
        var message = "Operation completed successfully";

        // Act
        var response = ApiResponse.CreateSuccess(message);

        // Assert
        Assert.True(response.Success);
        Assert.Equal(message, response.Message);
        Assert.Null(response.Errors);
    }

    [Fact]
    public void ApiResponse_FailureResult_ShouldCreateFailureResponse()
    {
        // Arrange
        var message = "Operation failed";
        var error = "An error occurred";

        // Act
        var response = ApiResponse.CreateFailure(message, error);

        // Assert
        Assert.False(response.Success);
        Assert.Equal(message, response.Message);
        Assert.NotNull(response.Errors);
        Assert.Contains(error, response.Errors);
    }

    [Fact]
    public void ApiResponseGeneric_SuccessResult_ShouldCreateSuccessResponse()
    {
        // Arrange
        var data = "test data";
        var message = "Operation successful";

        // Act
        var response = ApiResponse<string>.SuccessResult(data, message);

        // Assert
        Assert.True(response.Success);
        Assert.Equal(message, response.Message);
        Assert.Equal(data, response.Data);
        Assert.Null(response.Errors);
    }

    [Fact]
    public void ApiResponseGeneric_FailureResult_ShouldCreateFailureResponse()
    {
        // Arrange
        var message = "Operation failed";
        var errorMessage = "Specific error";

        // Act
        var response = ApiResponse<string>.FailureResult(message, errorMessage);

        // Assert
        Assert.False(response.Success);
        Assert.Equal(message, response.Message);
        Assert.Null(response.Data);
        Assert.NotNull(response.Errors);
        Assert.Contains(errorMessage, response.Errors);
    }

    [Fact]
    public void ApiResponseGeneric_SuccessResultWithoutMessage_ShouldCreateSuccessResponse()
    {
        // Arrange
        var data = "test data";

        // Act
        var response = ApiResponse<string>.SuccessResult(data);

        // Assert
        Assert.True(response.Success);
        Assert.Equal(data, response.Data);
        Assert.Null(response.Errors);
    }

    [Fact]
    public void ApiResponse_CreateSuccessWithoutMessage_ShouldCreateSuccessResponse()
    {
        // Act
        var response = ApiResponse.CreateSuccess();

        // Assert
        Assert.True(response.Success);
        Assert.Equal("Operation successful", response.Message);
        Assert.Null(response.Errors);
    }

    [Fact]
    public void ApiResponseGeneric_FailureResultWithMultipleErrors_ShouldCreateFailureResponse()
    {
        // Arrange
        var errors = new List<string> { "Error 1", "Error 2", "Error 3" };
        var message = "Operation failed with multiple errors";

        // Act
        var response = ApiResponse<string>.FailureResult(message, errors.AsReadOnly());

        // Assert
        Assert.False(response.Success);
        Assert.Equal(message, response.Message);
        Assert.Null(response.Data);
        Assert.NotNull(response.Errors);
        Assert.Equal(3, response.Errors.Count);
        Assert.Contains("Error 1", response.Errors);
        Assert.Contains("Error 2", response.Errors);
        Assert.Contains("Error 3", response.Errors);
    }

    [Fact]
    public void ApiResponse_CreateFailureWithMultipleErrors_ShouldCreateFailureResponse()
    {
        // Arrange
        var errors = new List<string> { "Error A", "Error B" };
        var message = "Multiple errors occurred";

        // Act
        var response = ApiResponse.CreateFailure(message, errors.AsReadOnly());

        // Assert
        Assert.False(response.Success);
        Assert.Equal(message, response.Message);
        Assert.NotNull(response.Errors);
        Assert.Equal(2, response.Errors.Count);
        Assert.Contains("Error A", response.Errors);
        Assert.Contains("Error B", response.Errors);
    }

    [Fact]
    public void ApiResponse_CreateSuccessWithMessage_ShouldCreateSuccessResponse()
    {
        // Arrange
        var message = "Custom success message";

        // Act
        var response = ApiResponse.CreateSuccess(message);

        // Assert
        Assert.True(response.Success);
        Assert.Equal(message, response.Message);
        Assert.Null(response.Errors);
    }

    [Fact]
    public void ApiResponse_CreateFailureWithSingleError_ShouldCreateFailureResponse()
    {
        // Arrange
        var message = "Operation failed";
        var error = "Single error message";

        // Act
        var response = ApiResponse.CreateFailure(message, error);

        // Assert
        Assert.False(response.Success);
        Assert.Equal(message, response.Message);
        Assert.NotNull(response.Errors);
        Assert.Single(response.Errors);
        Assert.Contains(error, response.Errors);
    }

    [Fact]
    public void ApiResponse_CreateFailureWithReadOnlyCollection_ShouldCreateFailureResponse()
    {
        // Arrange
        var message = "Operation failed";
        var errors = new List<string> { "Error 1", "Error 2" };
        var readOnlyErrors = new ReadOnlyCollection<string>(errors);

        // Act
        var response = ApiResponse.CreateFailure(message, readOnlyErrors);

        // Assert
        Assert.False(response.Success);
        Assert.Equal(message, response.Message);
        Assert.NotNull(response.Errors);
        Assert.Equal(2, response.Errors.Count);
        Assert.Contains("Error 1", response.Errors);
        Assert.Contains("Error 2", response.Errors);
    }

    [Fact]
    public void ApiResponse_NonGenericCreateFailureWithCollection_ShouldCreateFailureResponse()
    {
        // Arrange
        var errors = new ReadOnlyCollection<string>(new[] { "Error 1", "Error 2" });
        
        // Act
        var response = ApiResponse.CreateFailure("Test failure", errors);
        
        // Assert
        Assert.False(response.Success);
        Assert.Equal("Test failure", response.Message);
        Assert.Null(response.Data);
        Assert.Equal(errors, response.Errors);
    }

    [Fact]
    public void ApiResponse_NonGenericCreateFailureWithIReadOnlyCollection_ShouldCreateFailureResponse()
    {
        // Arrange
        IReadOnlyCollection<string> errors = new[] { "Error 1", "Error 2" };
        
        // Act
        var response = ApiResponse.CreateFailure("Test failure message", errors);
        
        // Assert
        Assert.False(response.Success);
        Assert.Equal("Test failure message", response.Message);
        Assert.Null(response.Data);
        Assert.Equal(errors, response.Errors);
    }

    [Fact]
    public void ApiResponse_CreateFailureWithNullErrors_ShouldCreateFailureResponse()
    {
        // Arrange
        var message = "Operation failed";
        IReadOnlyCollection<string>? errors = null;

        // Act
        var response = ApiResponse.CreateFailure(message, errors);

        // Assert
        Assert.False(response.Success);
        Assert.Equal(message, response.Message);
        Assert.Null(response.Errors);
    }

    [Fact]
    public void ApiResponse_DirectConstructorAccess_ShouldWork()
    {
        // Arrange
        var message = "Direct constructor test";
        var errors = new List<string> { "Error 1" };

        // Act
        var response = ApiResponse.CreateFailure(message, errors.AsReadOnly());

        // Assert
        Assert.False(response.Success);
        Assert.Equal(message, response.Message);
        Assert.NotNull(response.Errors);
        Assert.Contains("Error 1", response.Errors);
    }

    [Fact]
    public void ApiResponse_EmptyErrorsCollection_ShouldWork()
    {
        // Arrange
        var message = "Empty errors test";
        var errors = new List<string>();

        // Act
        var response = ApiResponse.CreateFailure(message, errors.AsReadOnly());

        // Assert
        Assert.False(response.Success);
        Assert.Equal(message, response.Message);
        Assert.NotNull(response.Errors);
        Assert.Empty(response.Errors);
    }
}
