using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Products;

namespace FSH.Modules.Catalog.Features.v1.Products.ChangeProductPrice;

public sealed class ChangeProductPriceCommandValidator : AbstractValidator<ChangeProductPriceCommand>
{
    public ChangeProductPriceCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}
