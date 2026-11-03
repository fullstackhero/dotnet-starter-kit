using Catalog.Tests.Support;
using FSH.Modules.Catalog.Contracts.v1.Products;
using FSH.Modules.Catalog.Features.v1.Products.AdjustProductStock;

namespace Catalog.Tests.Validators;

// Exercises the validator with a REAL IStringLocalizer<CatalogResources> so the localized
// message key ("Validation.DeltaNonZero") is proven to resolve against the embedded catalog.
public sealed class AdjustProductStockCommandValidatorTests
{
    private readonly AdjustProductStockCommandValidator _sut = new(CatalogResourcesLocalizerFactory.Create());

    [Fact]
    public void Delta_Should_Pass_When_NonZero()
    {
        var command = new AdjustProductStockCommand(Guid.NewGuid(), 5);

        var result = _sut.Validate(command);

        result.Errors.ShouldNotContain(e => e.PropertyName == nameof(AdjustProductStockCommand.Delta));
    }

    [Fact]
    public void Delta_Should_Fail_With_LocalizedMessage_When_Zero()
    {
        var command = new AdjustProductStockCommand(Guid.NewGuid(), 0);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors
            .Where(e => e.PropertyName == nameof(AdjustProductStockCommand.Delta))
            .ShouldContain(e => e.ErrorMessage == "Delta must be non-zero.");
    }
}
