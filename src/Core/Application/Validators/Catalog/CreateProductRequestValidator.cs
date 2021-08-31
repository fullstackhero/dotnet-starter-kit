using FluentValidation;
using DN.WebApi.Shared.DTOs.Catalog;

namespace DN.WebApi.Application.Validators.Catalog
{
    public class CreateProductRequestValidator : CustomValidator<CreateProductRequest>
    {
        public CreateProductRequestValidator()
        {
            RuleFor(p => p.Name).MaximumLength(75).NotEmpty();
            RuleFor(p => p.Rate).GreaterThanOrEqualTo(1).NotEqual(0);
        }
    }
}