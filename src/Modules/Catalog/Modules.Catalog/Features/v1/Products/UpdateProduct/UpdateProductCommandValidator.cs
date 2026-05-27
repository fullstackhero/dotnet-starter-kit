using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Products;

namespace FSH.Modules.Catalog.Features.v1.Products.UpdateProduct;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.BrandId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}
