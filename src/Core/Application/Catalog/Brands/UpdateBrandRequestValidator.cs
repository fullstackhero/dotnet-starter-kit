using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Common.Validation;
using DN.WebApi.Domain.Catalog.Brands;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace DN.WebApi.Application.Catalog.Brands;

public class UpdateBrandRequestValidator : CustomValidator<UpdateBrandRequest>
{
    public UpdateBrandRequestValidator(IRepositoryAsync repository, IStringLocalizer<UpdateBrandRequestValidator> localizer)
    {
        RuleFor(p => p.Name)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (brand, name, ct) => !await repository.ExistsAsync<Brand>(b => b.Id != brand.Id && b.Name == name, ct))
                .WithMessage((_, name) => string.Format(localizer["brand.alreadyexists"], name));
    }
}