using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Products.RemoveProductImage;

namespace FSH.Modules.Catalog.Features.v1.Products.RemoveProductImage;

public sealed class RemoveProductImageCommandValidator : AbstractValidator<RemoveProductImageCommand>
{
    public RemoveProductImageCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ImageId).NotEmpty();
    }
}
