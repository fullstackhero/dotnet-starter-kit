using DN.WebApi.Application.Common.Validation;
using FluentValidation;

namespace DN.WebApi.Host.Endpoints.Catalog.Brands;

public class CreateBrandRequestValidator : CustomValidator<CreateBrandRequest>
{
    public CreateBrandRequestValidator()
    {
        RuleFor(p => p.Name).MaximumLength(75).NotEmpty();
    }
}