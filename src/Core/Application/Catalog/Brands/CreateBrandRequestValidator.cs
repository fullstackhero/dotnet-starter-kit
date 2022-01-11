using DN.WebApi.Application.Common.Persistence;
using DN.WebApi.Application.Common.Validation;
using DN.WebApi.Domain.Catalog.Brands;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace DN.WebApi.Application.Catalog.Brands;

public class CreateBrandRequestValidator : CustomValidator<CreateBrandRequest>
{
    public CreateBrandRequestValidator(IReadRepository<Brand> repository, IStringLocalizer<CreateBrandRequestValidator> localizer)
    {
        RuleFor(p => p.Name)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (name, ct) => await repository.GetBySpecAsync(new BrandByNameSpec(name), ct) is null)
                .WithMessage((_, name) => string.Format(localizer["brand.alreadyexists"], name));
    }
}