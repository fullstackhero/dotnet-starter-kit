using DN.WebApi.Application.Common.Validators;
using DN.WebApi.Shared.DTOs.Catalog;
using FluentValidation;

namespace DN.WebApi.Application.Catalog.Validators;

public class CreateBrandRequestValidator : CustomValidator<CreateBrandRequest>
{
    public CreateBrandRequestValidator()
    {
        RuleFor(p => p.Name).MaximumLength(75).NotEmpty();
    }
}