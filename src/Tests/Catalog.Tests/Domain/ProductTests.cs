using FSH.Framework.Core.Domain;
using FSH.Modules.Catalog.Domain;
using FSH.Modules.Catalog.Domain.Events;

namespace Catalog.Tests.Domain;

public sealed class ProductTests
{
    private static Product CreateValidProduct(int stock = 10, decimal amount = 9.99m, string currency = "USD")
        => Product.Create(
            sku: "sku-001",
            name: "Test Product",
            description: " a description ",
            brandId: Guid.NewGuid(),
            categoryId: Guid.NewGuid(),
            price: new Money(amount, currency),
            stock: stock);

    #region Create - Happy Path

    [Fact]
    public void Create_Should_NormalizeSkuToUpperAndTrim_When_SkuHasMixedCaseAndWhitespace()
    {
        // Arrange / Act
        Product product = Product.Create("  abc-123  ", "Name", null, Guid.NewGuid(), Guid.NewGuid(), Money.Zero(), 0);

        // Assert
        product.Sku.ShouldBe("ABC-123");
    }

    [Fact]
    public void Create_Should_TrimNameAndGenerateSlug_When_NameHasWhitespaceAndSymbols()
    {
        // Arrange / Act
        Product product = Product.Create("sku", "  Hello World!!  ", null, Guid.NewGuid(), Guid.NewGuid(), Money.Zero(), 0);

        // Assert
        product.Name.ShouldBe("Hello World!!");
        product.Slug.ShouldBe("hello-world");
    }

    [Fact]
    public void Create_Should_TrimDescription_When_DescriptionProvided()
    {
        // Arrange / Act
        Product product = CreateValidProduct();

        // Assert
        product.Description.ShouldBe("a description");
    }

    [Fact]
    public void Create_Should_LeaveDescriptionNull_When_DescriptionIsNull()
    {
        // Arrange / Act
        Product product = Product.Create("sku", "Name", null, Guid.NewGuid(), Guid.NewGuid(), Money.Zero(), 0);

        // Assert
        product.Description.ShouldBeNull();
    }

    [Fact]
    public void Create_Should_BeActiveAndRaiseProductCreatedEvent_When_Valid()
    {
        // Arrange / Act
        Product product = CreateValidProduct();

        // Assert
        product.IsActive.ShouldBeTrue();
        product.Id.ShouldNotBe(Guid.Empty);
        IDomainEvent evt = product.DomainEvents.ShouldHaveSingleItem();
        ProductCreatedDomainEvent created = evt.ShouldBeOfType<ProductCreatedDomainEvent>();
        created.ProductId.ShouldBe(product.Id);
        created.Sku.ShouldBe(product.Sku);
        created.Name.ShouldBe(product.Name);
    }

    #endregion

    #region Create - Guards

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_SkuIsBlank(string sku)
    {
        // Act / Assert
        Should.Throw<ArgumentException>(() =>
            Product.Create(sku, "Name", null, Guid.NewGuid(), Guid.NewGuid(), Money.Zero(), 0));
    }

    [Fact]
    public void Create_Should_Throw_When_SkuIsNull()
    {
        // Act / Assert
        Should.Throw<ArgumentException>(() =>
            Product.Create(null!, "Name", null, Guid.NewGuid(), Guid.NewGuid(), Money.Zero(), 0));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_NameIsBlank(string name)
    {
        // Act / Assert
        Should.Throw<ArgumentException>(() =>
            Product.Create("sku", name, null, Guid.NewGuid(), Guid.NewGuid(), Money.Zero(), 0));
    }

    [Fact]
    public void Create_Should_Throw_When_PriceIsNull()
    {
        // Act / Assert
        Should.Throw<ArgumentNullException>(() =>
            Product.Create("sku", "Name", null, Guid.NewGuid(), Guid.NewGuid(), null!, 0));
    }

    [Fact]
    public void Create_Should_Throw_When_StockIsNegative()
    {
        // Act / Assert
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Product.Create("sku", "Name", null, Guid.NewGuid(), Guid.NewGuid(), Money.Zero(), -1));
    }

    [Fact]
    public void Create_Should_Throw_When_PriceIsNegative()
    {
        // Act / Assert - shared Money allows signed amounts; the non-negative price invariant lives on the aggregate
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Product.Create("sku", "Name", null, Guid.NewGuid(), Guid.NewGuid(), new Money(-0.01m, "USD"), 0));
    }

    [Fact]
    public void Create_Should_Throw_When_BrandIdIsEmpty()
    {
        // Act / Assert
        Should.Throw<ArgumentException>(() =>
            Product.Create("sku", "Name", null, Guid.Empty, Guid.NewGuid(), Money.Zero(), 0));
    }

    [Fact]
    public void Create_Should_Throw_When_CategoryIdIsEmpty()
    {
        // Act / Assert
        Should.Throw<ArgumentException>(() =>
            Product.Create("sku", "Name", null, Guid.NewGuid(), Guid.Empty, Money.Zero(), 0));
    }

    #endregion

    #region Update

    [Fact]
    public void Update_Should_MutateFieldsAndStampUpdatedAt_When_Valid()
    {
        // Arrange
        Product product = CreateValidProduct();
        Guid newBrand = Guid.NewGuid();
        Guid newCategory = Guid.NewGuid();

        // Act
        product.Update("  New Name  ", "  desc  ", newBrand, newCategory, isActive: false);

        // Assert
        product.Name.ShouldBe("New Name");
        product.Slug.ShouldBe("new-name");
        product.Description.ShouldBe("desc");
        product.BrandId.ShouldBe(newBrand);
        product.CategoryId.ShouldBe(newCategory);
        product.IsActive.ShouldBeFalse();
        product.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_Should_Throw_When_NameIsBlank(string name)
    {
        // Arrange
        Product product = CreateValidProduct();

        // Act / Assert
        Should.Throw<ArgumentException>(() =>
            product.Update(name, null, Guid.NewGuid(), Guid.NewGuid(), true));
    }

    [Fact]
    public void Update_Should_Throw_When_BrandIdIsEmpty()
    {
        // Arrange
        Product product = CreateValidProduct();

        // Act / Assert
        Should.Throw<ArgumentException>(() =>
            product.Update("Name", null, Guid.Empty, Guid.NewGuid(), true));
    }

    [Fact]
    public void Update_Should_Throw_When_CategoryIdIsEmpty()
    {
        // Arrange
        Product product = CreateValidProduct();

        // Act / Assert
        Should.Throw<ArgumentException>(() =>
            product.Update("Name", null, Guid.NewGuid(), Guid.Empty, true));
    }

    #endregion

    #region ChangePrice

    [Fact]
    public void ChangePrice_Should_RaiseEventWithOldAndNewAmounts_When_PriceDiffers()
    {
        // Arrange
        Product product = CreateValidProduct(amount: 9.99m, currency: "USD");
        product.ClearDomainEvents();
        var newPrice = new Money(19.99m, "EUR");

        // Act
        product.ChangePrice(newPrice);

        // Assert
        product.Price.ShouldBe(newPrice);
        ProductPriceChangedDomainEvent evt = product.DomainEvents
            .ShouldHaveSingleItem()
            .ShouldBeOfType<ProductPriceChangedDomainEvent>();
        evt.OldAmount.ShouldBe(9.99m);
        evt.NewAmount.ShouldBe(19.99m);
        evt.Currency.ShouldBe("EUR");
    }

    [Fact]
    public void ChangePrice_Should_BeNoOp_When_PriceEqualsCurrent()
    {
        // Arrange
        Product product = CreateValidProduct(amount: 9.99m, currency: "USD");
        product.ClearDomainEvents();

        // Act - equal value (records compare by value; currency normalized to upper)
        product.ChangePrice(new Money(9.99m, "usd"));

        // Assert
        product.DomainEvents.ShouldBeEmpty();
        product.UpdatedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void ChangePrice_Should_Throw_When_NewPriceIsNull()
    {
        // Arrange
        Product product = CreateValidProduct();

        // Act / Assert
        Should.Throw<ArgumentNullException>(() => product.ChangePrice(null!));
    }

    [Fact]
    public void ChangePrice_Should_Throw_When_NewPriceIsNegative()
    {
        // Arrange
        Product product = CreateValidProduct();

        // Act / Assert
        Should.Throw<ArgumentOutOfRangeException>(() => product.ChangePrice(new Money(-1m, "USD")));
    }

    #endregion

    #region AdjustStock

    [Fact]
    public void AdjustStock_Should_IncreaseStockAndRaiseEvent_When_DeltaPositive()
    {
        // Arrange
        Product product = CreateValidProduct(stock: 10);
        product.ClearDomainEvents();

        // Act
        product.AdjustStock(5);

        // Assert
        product.Stock.ShouldBe(15);
        ProductStockAdjustedDomainEvent evt = product.DomainEvents
            .ShouldHaveSingleItem()
            .ShouldBeOfType<ProductStockAdjustedDomainEvent>();
        evt.OldStock.ShouldBe(10);
        evt.NewStock.ShouldBe(15);
        evt.Delta.ShouldBe(5);
    }

    [Fact]
    public void AdjustStock_Should_DecreaseStock_When_DeltaNegativeButResultNonNegative()
    {
        // Arrange
        Product product = CreateValidProduct(stock: 10);

        // Act
        product.AdjustStock(-10);

        // Assert
        product.Stock.ShouldBe(0);
    }

    [Fact]
    public void AdjustStock_Should_Throw_When_ResultWouldBeNegative()
    {
        // Arrange
        Product product = CreateValidProduct(stock: 3);

        // Act / Assert
        Should.Throw<InvalidOperationException>(() => product.AdjustStock(-4));
    }

    [Fact]
    public void AdjustStock_Should_NotMutateStock_When_AdjustmentThrows()
    {
        // Arrange
        Product product = CreateValidProduct(stock: 3);

        // Act
        Should.Throw<InvalidOperationException>(() => product.AdjustStock(-4));

        // Assert
        product.Stock.ShouldBe(3);
    }

    #endregion

    #region AddImage / Thumbnail invariants

    [Fact]
    public void AddImage_Should_MarkFirstImageAsThumbnailWithSortZero_When_NoImagesExist()
    {
        // Arrange
        Product product = CreateValidProduct();

        // Act
        ProductImage image = product.AddImage(Guid.NewGuid(), "https://cdn/img1.png");

        // Assert
        image.IsThumbnail.ShouldBeTrue();
        image.SortOrder.ShouldBe(0);
        product.ThumbnailUrl.ShouldBe("https://cdn/img1.png");
        product.Images.Count.ShouldBe(1);
    }

    [Fact]
    public void AddImage_Should_NotMarkAsThumbnailAndIncrementSortOrder_When_ImagesAlreadyExist()
    {
        // Arrange
        Product product = CreateValidProduct();
        product.AddImage(null, "https://cdn/img1.png");

        // Act
        ProductImage second = product.AddImage(null, "https://cdn/img2.png");

        // Assert
        second.IsThumbnail.ShouldBeFalse();
        second.SortOrder.ShouldBe(1);
    }

    [Fact]
    public void AddImage_Should_Throw_When_UrlIsBlank()
    {
        // Arrange
        Product product = CreateValidProduct();

        // Act / Assert
        Should.Throw<ArgumentException>(() => product.AddImage(null, "   "));
    }

    [Fact]
    public void ThumbnailUrl_Should_BeNull_When_NoImages()
    {
        // Arrange
        Product product = CreateValidProduct();

        // Assert
        product.ThumbnailUrl.ShouldBeNull();
    }

    #endregion

    #region RemoveImage

    [Fact]
    public void RemoveImage_Should_PromoteLowestSortedImageToThumbnail_When_ThumbnailRemovedAndOthersRemain()
    {
        // Arrange
        Product product = CreateValidProduct();
        ProductImage first = product.AddImage(null, "https://cdn/1.png");  // thumbnail, sort 0
        ProductImage second = product.AddImage(null, "https://cdn/2.png"); // sort 1
        ProductImage third = product.AddImage(null, "https://cdn/3.png");  // sort 2

        // Act - remove the thumbnail
        product.RemoveImage(first.Id);

        // Assert - lowest sorted remaining (second, sort 1) is promoted
        second.IsThumbnail.ShouldBeTrue();
        third.IsThumbnail.ShouldBeFalse();
        product.ThumbnailUrl.ShouldBe("https://cdn/2.png");
    }

    [Fact]
    public void RemoveImage_Should_NotPromote_When_RemovedImageWasNotThumbnail()
    {
        // Arrange
        Product product = CreateValidProduct();
        ProductImage first = product.AddImage(null, "https://cdn/1.png");  // thumbnail
        ProductImage second = product.AddImage(null, "https://cdn/2.png"); // non-thumbnail

        // Act
        product.RemoveImage(second.Id);

        // Assert
        first.IsThumbnail.ShouldBeTrue();
        product.Images.Count.ShouldBe(1);
    }

    [Fact]
    public void RemoveImage_Should_LeaveProductWithNoThumbnail_When_LastImageRemoved()
    {
        // Arrange
        Product product = CreateValidProduct();
        ProductImage only = product.AddImage(null, "https://cdn/1.png");

        // Act
        product.RemoveImage(only.Id);

        // Assert
        product.Images.ShouldBeEmpty();
        product.ThumbnailUrl.ShouldBeNull();
    }

    [Fact]
    public void RemoveImage_Should_Throw_When_ImageNotFound()
    {
        // Arrange
        Product product = CreateValidProduct();

        // Act / Assert
        Should.Throw<InvalidOperationException>(() => product.RemoveImage(Guid.NewGuid()));
    }

    #endregion

    #region SetThumbnail

    [Fact]
    public void SetThumbnail_Should_MoveThumbnailFlagToTarget_When_TargetIsNotCurrentThumbnail()
    {
        // Arrange
        Product product = CreateValidProduct();
        ProductImage first = product.AddImage(null, "https://cdn/1.png");  // thumbnail
        ProductImage second = product.AddImage(null, "https://cdn/2.png");

        // Act
        product.SetThumbnail(second.Id);

        // Assert - exactly one thumbnail, and it is the target
        first.IsThumbnail.ShouldBeFalse();
        second.IsThumbnail.ShouldBeTrue();
        product.Images.Count(i => i.IsThumbnail).ShouldBe(1);
    }

    [Fact]
    public void SetThumbnail_Should_BeNoOp_When_TargetIsAlreadyThumbnail()
    {
        // Arrange
        Product product = CreateValidProduct();
        ProductImage first = product.AddImage(null, "https://cdn/1.png");
        product.AddImage(null, "https://cdn/2.png");
        DateTime? before = product.UpdatedAtUtc;

        // Act
        product.SetThumbnail(first.Id);

        // Assert - unchanged timestamp signals the early-return branch
        product.UpdatedAtUtc.ShouldBe(before);
        first.IsThumbnail.ShouldBeTrue();
    }

    [Fact]
    public void SetThumbnail_Should_Throw_When_ImageNotFound()
    {
        // Arrange
        Product product = CreateValidProduct();
        product.AddImage(null, "https://cdn/1.png");

        // Act / Assert
        Should.Throw<InvalidOperationException>(() => product.SetThumbnail(Guid.NewGuid()));
    }

    #endregion

    #region ReorderImages

    [Fact]
    public void ReorderImages_Should_AssignSortOrderInSuppliedSequence_When_AllIdsProvided()
    {
        // Arrange
        Product product = CreateValidProduct();
        ProductImage a = product.AddImage(null, "https://cdn/a.png"); // sort 0
        ProductImage b = product.AddImage(null, "https://cdn/b.png"); // sort 1
        ProductImage c = product.AddImage(null, "https://cdn/c.png"); // sort 2

        // Act - reverse order
        product.ReorderImages([c.Id, b.Id, a.Id]);

        // Assert
        c.SortOrder.ShouldBe(0);
        b.SortOrder.ShouldBe(1);
        a.SortOrder.ShouldBe(2);
    }

    [Fact]
    public void ReorderImages_Should_AppendTrailingImages_When_SequenceIsPartial()
    {
        // Arrange
        Product product = CreateValidProduct();
        ProductImage a = product.AddImage(null, "https://cdn/a.png"); // sort 0
        ProductImage b = product.AddImage(null, "https://cdn/b.png"); // sort 1
        ProductImage c = product.AddImage(null, "https://cdn/c.png"); // sort 2

        // Act - only reorder c first; a and b are trailing, kept in existing sort order
        product.ReorderImages([c.Id]);

        // Assert
        c.SortOrder.ShouldBe(0);
        a.SortOrder.ShouldBe(1);
        b.SortOrder.ShouldBe(2);
    }

    [Fact]
    public void ReorderImages_Should_IgnoreUnknownIds_When_SequenceContainsIdsNotOnProduct()
    {
        // Arrange
        Product product = CreateValidProduct();
        ProductImage a = product.AddImage(null, "https://cdn/a.png");

        // Act - unknown id is skipped (continue branch), then trailing 'a' appended
        product.ReorderImages([Guid.NewGuid(), a.Id]);

        // Assert
        a.SortOrder.ShouldBe(0);
    }

    [Fact]
    public void ReorderImages_Should_Throw_When_OrderedIdsIsNull()
    {
        // Arrange
        Product product = CreateValidProduct();

        // Act / Assert
        Should.Throw<ArgumentNullException>(() => product.ReorderImages(null!));
    }

    #endregion

    #region Restore (soft-delete)

    [Fact]
    public void Restore_Should_BeNoOp_When_NotDeleted()
    {
        // Arrange
        Product product = CreateValidProduct();

        // Act
        product.Restore();

        // Assert - early-return branch; never marked updated by restore
        product.IsDeleted.ShouldBeFalse();
        product.UpdatedAtUtc.ShouldBeNull();
    }

    #endregion
}
