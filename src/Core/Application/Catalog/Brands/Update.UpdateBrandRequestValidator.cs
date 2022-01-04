using DN.WebApi.Application.Common.Validation;
using FluentValidation;

namespace DN.WebApi.Application.Catalog.Brands;

public class UpdateBrandRequestValidator : CustomValidator<UpdateBrandRequest>
{
    public UpdateBrandRequestValidator()
    {
        RuleFor(p => p.Name).MaximumLength(75).NotEmpty();
    }
}