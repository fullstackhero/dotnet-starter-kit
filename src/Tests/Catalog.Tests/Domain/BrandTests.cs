using FSH.Modules.Catalog.Domain;

namespace Catalog.Tests.Domain;

public sealed class BrandTests
{
    #region Create

    [Fact]
    public void Create_Should_TrimFieldsAndSlugify_When_Valid()
    {
        // Act
        Brand brand = Brand.Create("  Acme Corp.  ", "  desc  ", "  https://cdn/logo.png  ");

        // Assert
        brand.Name.ShouldBe("Acme Corp.");
        brand.Slug.ShouldBe("acme-corp");
        brand.Description.ShouldBe("desc");
        brand.LogoUrl.ShouldBe("https://cdn/logo.png");
        brand.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Create_Should_LeaveOptionalFieldsNull_When_NotProvided()
    {
        // Act
        Brand brand = Brand.Create("Acme", null, null);

        // Assert
        brand.Description.ShouldBeNull();
        brand.LogoUrl.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_NameIsBlank(string name)
    {
        // Act / Assert
        Should.Throw<ArgumentException>(() => Brand.Create(name, null, null));
    }

    [Fact]
    public void Create_Should_Throw_When_NameIsNull()
    {
        // Act / Assert
        Should.Throw<ArgumentException>(() => Brand.Create(null!, null, null));
    }

    #endregion

    #region Update

    [Fact]
    public void Update_Should_MutateFieldsAndRestamp_When_Valid()
    {
        // Arrange
        Brand brand = Brand.Create("Acme", null, null);

        // Act
        brand.Update("  New Brand  ", "  new desc  ", "  https://cdn/new.png  ");

        // Assert
        brand.Name.ShouldBe("New Brand");
        brand.Slug.ShouldBe("new-brand");
        brand.Description.ShouldBe("new desc");
        brand.LogoUrl.ShouldBe("https://cdn/new.png");
        brand.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Update_Should_ClearOptionalFields_When_NullsProvided()
    {
        // Arrange
        Brand brand = Brand.Create("Acme", "desc", "https://cdn/logo.png");

        // Act
        brand.Update("Acme", null, null);

        // Assert
        brand.Description.ShouldBeNull();
        brand.LogoUrl.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_Should_Throw_When_NameIsBlank(string name)
    {
        // Arrange
        Brand brand = Brand.Create("Acme", null, null);

        // Act / Assert
        Should.Throw<ArgumentException>(() => brand.Update(name, null, null));
    }

    #endregion

    #region Restore

    [Fact]
    public void Restore_Should_BeNoOp_When_NotDeleted()
    {
        // Arrange
        Brand brand = Brand.Create("Acme", null, null);

        // Act
        brand.Restore();

        // Assert
        brand.IsDeleted.ShouldBeFalse();
        brand.UpdatedAtUtc.ShouldBeNull();
    }

    #endregion
}
