using FSH.Modules.Catalog.Domain;

namespace Catalog.Tests.Domain;

public sealed class CategoryTests
{
    #region Create

    [Fact]
    public void Create_Should_TrimNameSlugifyAndKeepParent_When_Valid()
    {
        // Arrange
        Guid parent = Guid.NewGuid();

        // Act
        Category category = Category.Create("  Power Tools & More  ", "  desc  ", parent);

        // Assert
        category.Name.ShouldBe("Power Tools & More");
        category.Slug.ShouldBe("power-tools-more");
        category.Description.ShouldBe("desc");
        category.ParentCategoryId.ShouldBe(parent);
        category.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Create_Should_LeaveDescriptionAndParentNull_When_NotProvided()
    {
        // Act
        Category category = Category.Create("Root", null, null);

        // Assert
        category.Description.ShouldBeNull();
        category.ParentCategoryId.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_NameIsBlank(string name)
    {
        // Act / Assert
        Should.Throw<ArgumentException>(() => Category.Create(name, null, null));
    }

    [Fact]
    public void Create_Should_Throw_When_NameIsNull()
    {
        // Act / Assert
        Should.Throw<ArgumentException>(() => Category.Create(null!, null, null));
    }

    #endregion

    #region Update - cycle / parent guard

    [Fact]
    public void Update_Should_Throw_When_ParentEqualsOwnId()
    {
        // Arrange
        Category category = Category.Create("Cat", null, null);

        // Act / Assert - a category cannot be its own parent
        Should.Throw<InvalidOperationException>(() =>
            category.Update("Cat", null, category.Id));
    }

    [Fact]
    public void Update_Should_ReparentAndRestamp_When_ParentIsDifferentId()
    {
        // Arrange
        Category category = Category.Create("Cat", null, null);
        Guid newParent = Guid.NewGuid();

        // Act
        category.Update("  Renamed  ", "  new  ", newParent);

        // Assert
        category.Name.ShouldBe("Renamed");
        category.Slug.ShouldBe("renamed");
        category.Description.ShouldBe("new");
        category.ParentCategoryId.ShouldBe(newParent);
        category.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Update_Should_AllowNullParent_When_PromotingToRoot()
    {
        // Arrange
        Category category = Category.Create("Cat", null, Guid.NewGuid());

        // Act
        category.Update("Cat", null, null);

        // Assert
        category.ParentCategoryId.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_Should_Throw_When_NameIsBlank(string name)
    {
        // Arrange
        Category category = Category.Create("Cat", null, null);

        // Act / Assert
        Should.Throw<ArgumentException>(() => category.Update(name, null, null));
    }

    #endregion

    #region Restore

    [Fact]
    public void Restore_Should_BeNoOp_When_NotDeleted()
    {
        // Arrange
        Category category = Category.Create("Cat", null, null);

        // Act
        category.Restore();

        // Assert
        category.IsDeleted.ShouldBeFalse();
        category.UpdatedAtUtc.ShouldBeNull();
    }

    #endregion
}
