using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Common.Validation;
using DN.WebApi.Application.FileStorage;
using DN.WebApi.Domain.Catalog;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace DN.WebApi.Application.Catalog.Products;

public class CreateProductRequestValidator : CustomValidator<CreateProductRequest>
{
    public CreateProductRequestValidator(IRepositoryAsync repository, IStringLocalizer<CreateProductRequestValidator> localizer)
    {
        RuleFor(p => p.Name)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (name, ct) => !await repository.ExistsAsync<Product>(p => p.Name == name, ct))
                .WithMessage((_, name) => string.Format(localizer["product.alreadyexists"], name));

        RuleFor(p => p.Rate)
            .GreaterThanOrEqualTo(1);

        RuleFor(p => p.Image)
            .SetNonNullableValidator(new FileUploadRequestValidator());

        RuleFor(p => p.BrandId)
            .NotEmpty()
            .MustAsync((id, ct) => repository.ExistsAsync<Brand>(a => a.Id == id, ct))
                .WithMessage((_, id) => string.Format(localizer["brand.notfound"], id));
    }
}