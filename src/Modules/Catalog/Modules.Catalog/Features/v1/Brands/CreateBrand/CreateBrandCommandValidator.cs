using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Brands;

namespace FSH.Modules.Catalog.Features.v1.Brands.CreateBrand;

public sealed class CreateBrandCommandValidator : AbstractValidator<CreateBrandCommand>
{
    public CreateBrandCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(1024);
        RuleFor(x => x.LogoUrl).MaximumLength(512);
    }
}
