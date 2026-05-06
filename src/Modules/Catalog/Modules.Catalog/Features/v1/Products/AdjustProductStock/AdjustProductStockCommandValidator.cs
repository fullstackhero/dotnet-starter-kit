using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Products;

namespace FSH.Modules.Catalog.Features.v1.Products.AdjustProductStock;

public sealed class AdjustProductStockCommandValidator : AbstractValidator<AdjustProductStockCommand>
{
    public AdjustProductStockCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Delta).NotEqual(0).WithMessage("Delta must be non-zero.");
    }
}
