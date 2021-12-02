using DN.WebApi.Application.Common.Validation;
using DN.WebApi.Application.FileStorage;
using DN.WebApi.Shared.DTOs.Catalog;
using FluentValidation;

namespace DN.WebApi.Application.Catalog.Validators;

public class CreateProductRequestValidator : CustomValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(p => p.Name).MaximumLength(75).NotEmpty();
        RuleFor(p => p.Rate).GreaterThanOrEqualTo(1).NotEqual(0);
        RuleFor(p => p.Image).SetNonNullableValidator(new FileUploadRequestValidator());
        RuleFor(p => p.BrandId).NotEmpty().NotNull();
    }
}