using DN.WebApi.Application.Common.Validation;
using FluentValidation;

namespace DN.WebApi.Host.Endpoints.Catalog.Brands;

public class UpdateBrandRequestValidator : CustomValidator<UpdateBrandRequest>
{
    public UpdateBrandRequestValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty()
            .MaximumLength(75);
    }
}