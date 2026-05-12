using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Products.AddProductImage;

namespace FSH.Modules.Catalog.Features.v1.Products.AddProductImage;

public sealed class AddProductImageCommandValidator : AbstractValidator<AddProductImageCommand>
{
    public AddProductImageCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2048);
    }
}
