using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Products;
using FSH.Modules.Catalog.Localization;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Catalog.Features.v1.Products.AdjustProductStock;

public sealed class AdjustProductStockCommandValidator : AbstractValidator<AdjustProductStockCommand>
{
    public AdjustProductStockCommandValidator(IStringLocalizer<CatalogResources> localizer)
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Delta).NotEqual(0).WithMessage(_ => localizer["Validation.DeltaNonZero"]);
    }
}
