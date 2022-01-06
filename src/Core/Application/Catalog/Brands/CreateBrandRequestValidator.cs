using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Common.Validation;
using DN.WebApi.Domain.Catalog;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace DN.WebApi.Application.Catalog.Brands;

public class CreateBrandRequestValidator : CustomValidator<CreateBrandRequest>
{
    private readonly IRepositoryAsync _repository;
    private readonly IStringLocalizer<CreateBrandRequestValidator> _localizer;

    public CreateBrandRequestValidator(IRepositoryAsync repository, IStringLocalizer<CreateBrandRequestValidator> localizer)
    {
        _repository = repository;
        _localizer = localizer;

        RuleFor(p => p.Name)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (name, ct) => !await _repository.ExistsAsync<Brand>(b => b.Name == name, ct))
                .WithMessage((_, name) => string.Format(_localizer["brand.alreadyexists"], name));
    }
}