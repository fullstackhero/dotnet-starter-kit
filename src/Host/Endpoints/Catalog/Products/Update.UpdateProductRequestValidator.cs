using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Common.Validation;
using DN.WebApi.Application.FileStorage;
using DN.WebApi.Domain.Catalog;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace DN.WebApi.Host.Endpoints.Catalog.Products;

public class UpdateProductRequestValidator : CustomValidator<UpdateProductRequest>
{
    private readonly IRepositoryAsync _repository;
    private readonly IStringLocalizer<UpdateProductRequestValidator> _localizer;

    public UpdateProductRequestValidator(IRepositoryAsync repository, IStringLocalizer<UpdateProductRequestValidator> localizer)
    {
        _repository = repository;
        _localizer = localizer;

        RuleFor(p => p.Name)
            .NotEmpty()
            .MaximumLength(75);

        RuleFor(p => p.Rate)
            .GreaterThanOrEqualTo(1);

        RuleFor(p => p.Image)
            .SetNonNullableValidator(new FileUploadRequestValidator());

        RuleFor(p => p.BrandId)
            .NotEmpty()
            .MustAsync((id, ct) => _repository.ExistsAsync<Brand>(a => a.Id == id, ct))
                .WithMessage(id => string.Format(_localizer["brand.notfound"], id));
    }
}