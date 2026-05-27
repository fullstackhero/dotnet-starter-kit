using FSH.Modules.Auditing.Contracts.v1.GetAudits;
using FSH.Modules.Auditing.Features.v1.GetAudits;
using FSH.Modules.Identity.Contracts.v1.Users.SearchUsers;
using FSH.Modules.Identity.Features.v1.Users.SearchUsers;

namespace Generic.Tests.Validators;

/// <summary>
/// Tests for generic paged query validation rules (PageNumber, PageSize)
/// that are shared across all modules implementing IPagedQuery.
/// </summary>
public sealed class PagedQueryValidatorTests
{
    public static TheoryData<IValidator, object> PagedQueryValidators => new()
    {
        { new GetAuditsQueryValidator(), new GetAuditsQuery() },
        { new SearchUsersQueryValidator(), new SearchUsersQuery() }
    };

    [Theory]
    [MemberData(nameof(PagedQueryValidators))]
    public void PageNumber_Should_Pass_When_Null(IValidator validator, object query)
    {
        // Arrange - PageNumber is null by default
        ArgumentNullException.ThrowIfNull(validator);

        // Act
        var result = validator.Validate(new ValidationContext<object>(query));

        // Assert
        result.Errors.ShouldNotContain(e => e.PropertyName == "PageNumber");
    }

    [Fact]
    public void PageNumber_Should_Pass_When_GreaterThanZero_Auditing()
    {
        // Arrange
        var validator = new GetAuditsQueryValidator();
        var query = new GetAuditsQuery { PageNumber = 1 };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.Errors.ShouldNotContain(e => e.PropertyName == "PageNumber");
    }

    [Fact]
    public void PageNumber_Should_Pass_When_GreaterThanZero_Identity()
    {
        // Arrange
        var validator = new SearchUsersQueryValidator();
        var query = new SearchUsersQuery { PageNumber = 5 };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.Errors.ShouldNotContain(e => e.PropertyName == "PageNumber");
    }

    [Fact]
    public void PageNumber_Should_Fail_When_Zero_Auditing()
    {
        // Arrange
        var validator = new GetAuditsQueryValidator();
        var query = new GetAuditsQuery { PageNumber = 0 };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.Errors.ShouldContain(e => e.PropertyName == "PageNumber");
    }

    [Fact]
    public void PageNumber_Should_Fail_When_Zero_Identity()
    {
        // Arrange
        var validator = new SearchUsersQueryValidator();
        var query = new SearchUsersQuery { PageNumber = 0 };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.Errors.ShouldContain(e => e.PropertyName == "PageNumber");
    }

    [Fact]
    public void PageNumber_Should_Fail_When_Negative_Auditing()
    {
        // Arrange
        var validator = new GetAuditsQueryValidator();
        var query = new GetAuditsQuery { PageNumber = -1 };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.Errors.ShouldContain(e => e.PropertyName == "PageNumber");
    }

    [Fact]
    public void PageNumber_Should_Fail_When_Negative_Identity()
    {
        // Arrange
        var validator = new SearchUsersQueryValidator();
        var query = new SearchUsersQuery { PageNumber = -5 };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.Errors.ShouldContain(e => e.PropertyName == "PageNumber");
    }

    [Fact]
    public void PageSize_Should_Pass_When_Null_Auditing()
    {
        // Arrange
        var validator = new GetAuditsQueryValidator();
        var query = new GetAuditsQuery { PageSize = null };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.Errors.ShouldNotContain(e => e.PropertyName == "PageSize");
    }

    [Fact]
    public void PageSize_Should_Pass_When_Null_Identity()
    {
        // Arrange
        var validator = new SearchUsersQueryValidator();
        var query = new SearchUsersQuery { PageSize = null };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.Errors.ShouldNotContain(e => e.PropertyName == "PageSize");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void PageSize_Should_Pass_When_Between1And100_Auditing(int pageSize)
    {
        // Arrange
        var validator = new GetAuditsQueryValidator();
        var query = new GetAuditsQuery { PageSize = pageSize };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.Errors.ShouldNotContain(e => e.PropertyName == "PageSize");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void PageSize_Should_Pass_When_Between1And100_Identity(int pageSize)
    {
        // Arrange
        var validator = new SearchUsersQueryValidator();
        var query = new SearchUsersQuery { PageSize = pageSize };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.Errors.ShouldNotContain(e => e.PropertyName == "PageSize");
    }

    [Fact]
    public void PageSize_Should_Fail_When_Zero_Auditing()
    {
        // Arrange
        var validator = new GetAuditsQueryValidator();
        var query = new GetAuditsQuery { PageSize = 0 };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.Errors.ShouldContain(e => e.PropertyName == "PageSize");
    }

    [Fact]
    public void PageSize_Should_Fail_When_Zero_Identity()
    {
        // Arrange
        var validator = new SearchUsersQueryValidator();
        var query = new SearchUsersQuery { PageSize = 0 };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.Errors.ShouldContain(e => e.PropertyName == "PageSize");
    }

    [Fact]
    public void PageSize_Should_Fail_When_GreaterThan100_Auditing()
    {
        // Arrange
        var validator = new GetAuditsQueryValidator();
        var query = new GetAuditsQuery { PageSize = 101 };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.Errors.ShouldContain(e => e.PropertyName == "PageSize");
    }

    [Fact]
    public void PageSize_Should_Fail_When_GreaterThan100_Identity()
    {
        // Arrange
        var validator = new SearchUsersQueryValidator();
        var query = new SearchUsersQuery { PageSize = 150 };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.Errors.ShouldContain(e => e.PropertyName == "PageSize");
    }
}