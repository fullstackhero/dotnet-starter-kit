using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Common.Validation;
using DN.WebApi.Domain.Catalog.Brands;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace DN.WebApi.Application.Catalog.Brands;

public class CreateBrandRequestValidator : CustomValidator<CreateBrandRequest>
{
    public CreateBrandRequestValidator(IRepositoryAsync repository, IStringLocalizer<CreateBrandRequestValidator> localizer)
    {
        RuleFor(p => p.Name)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (name, ct) => !await repository.ExistsAsync<Brand>(b => b.Name == name, ct))
                .WithMessage((_, name) => string.Format(localizer["brand.alreadyexists"], name));
    }
}