using System.Linq.Expressions;
using FSH.Framework.Persistence;
using FSH.Framework.Persistence.Specifications;

namespace Framework.Tests.Persistence;

public sealed class SpecificationTests
{
    #region Test doubles

    private sealed class Person
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public Person? Manager { get; set; }
    }

    // Test spec that exposes the protected composition helpers.
    private sealed class TestSpec : Specification<Person>
    {
        public void AddWhere(Expression<Func<Person, bool>> predicate) => Where(predicate);
        public void AddInclude(Expression<Func<Person, object>> include) => Include(include);
        public void AddInclude(string include) => Include(include);
        public void AddOrderBy(Expression<Func<Person, object>> key) => OrderBy(key);
        public void AddOrderByDescending(Expression<Func<Person, object>> key) => OrderByDescending(key);
        public void AddThenBy(Expression<Func<Person, object>> key) => ThenBy(key);

        public void Sort(string? expr, Action defaultOrdering, IReadOnlyDictionary<string, Expression<Func<Person, object>>> mappings)
            => ApplySortingOverride(expr, defaultOrdering, mappings);
    }

    private static readonly Person[] People =
    [
        new() { Id = 1, Name = "Alice", Age = 30 },
        new() { Id = 2, Name = "Bob", Age = 25 },
        new() { Id = 3, Name = "Carol", Age = 30 },
    ];

    #endregion

    #region Defaults

    [Fact]
    public void Specification_Should_DefaultToNoTracking_When_Constructed()
    {
        // Arrange & Act
        var spec = new TestSpec();

        // Assert
        spec.AsNoTracking.ShouldBeTrue();
        spec.AsSplitQuery.ShouldBeFalse();
        spec.IgnoreQueryFilters.ShouldBeFalse();
        spec.Criteria.ShouldBeNull();
        spec.Includes.ShouldBeEmpty();
        spec.OrderExpressions.ShouldBeEmpty();
    }

    #endregion

    #region Criteria

    [Fact]
    public void Criteria_Should_CombineWithLogicalAnd_When_MultipleWhereAdded()
    {
        // Arrange
        var spec = new TestSpec();
        spec.AddWhere(p => p.Age >= 30);
        spec.AddWhere(p => p.Name.StartsWith('A'));

        // Act — compile the combined criteria and run against in-memory data.
        var predicate = spec.Criteria.ShouldNotBeNull().Compile();
        var matches = People.Where(predicate).ToList();

        // Assert — only Alice satisfies both clauses.
        matches.Count.ShouldBe(1);
        matches[0].Name.ShouldBe("Alice");
    }

    [Fact]
    public void Criteria_Should_ReturnSingleExpression_When_OneWhereAdded()
    {
        // Arrange
        var spec = new TestSpec();
        spec.AddWhere(p => p.Age == 25);

        // Act
        var predicate = spec.Criteria.ShouldNotBeNull().Compile();

        // Assert
        People.Count(predicate).ShouldBe(1);
    }

    #endregion

    #region Includes

    [Fact]
    public void Include_Should_RegisterTypedAndStringIncludes_When_Added()
    {
        // Arrange
        var spec = new TestSpec();
        spec.AddInclude(p => p.Manager!);
        spec.AddInclude("Manager.Manager");

        // Assert
        spec.Includes.Count.ShouldBe(1);
        spec.IncludeStrings.ShouldHaveSingleItem();
        spec.IncludeStrings[0].ShouldBe("Manager.Manager");
    }

    #endregion

    #region Ordering

    [Fact]
    public void OrderExpressions_Should_RecordDirection_When_OrderHelpersUsed()
    {
        // Arrange
        var spec = new TestSpec();
        spec.AddOrderByDescending(p => p.Age);
        spec.AddThenBy(p => p.Name);

        // Assert
        spec.OrderExpressions.Count.ShouldBe(2);
        spec.OrderExpressions[0].Descending.ShouldBeTrue();
        spec.OrderExpressions[1].Descending.ShouldBeFalse();
    }

    [Fact]
    public void ApplySortingOverride_Should_UseClientSort_When_ValidExpressionProvided()
    {
        // Arrange
        var spec = new TestSpec();
        var defaultCalled = false;
        var mappings = new Dictionary<string, Expression<Func<Person, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = p => p.Name,
            ["age"] = p => p.Age,
        };

        // Act — "-age,name" => descending age, then ascending name.
        spec.Sort("-age,name", () => defaultCalled = true, mappings);

        // Assert
        defaultCalled.ShouldBeFalse();
        spec.OrderExpressions.Count.ShouldBe(2);
        spec.OrderExpressions[0].Descending.ShouldBeTrue();
        spec.OrderExpressions[1].Descending.ShouldBeFalse();
    }

    [Fact]
    public void ApplySortingOverride_Should_FallBackToDefault_When_ExpressionBlank()
    {
        // Arrange
        var spec = new TestSpec();
        var defaultCalled = false;
        var mappings = new Dictionary<string, Expression<Func<Person, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = p => p.Name,
        };

        // Act
        spec.Sort("   ", () => defaultCalled = true, mappings);

        // Assert
        defaultCalled.ShouldBeTrue();
    }

    [Fact]
    public void ApplySortingOverride_Should_FallBackToDefault_When_AllKeysInvalid()
    {
        // Arrange
        var spec = new TestSpec();
        var defaultCalled = false;
        var mappings = new Dictionary<string, Expression<Func<Person, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = p => p.Name,
        };

        // Act — none of the requested keys are whitelisted.
        spec.Sort("unknown,-bogus", () => defaultCalled = true, mappings);

        // Assert
        defaultCalled.ShouldBeTrue();
        spec.OrderExpressions.ShouldBeEmpty();
    }

    #endregion
}
