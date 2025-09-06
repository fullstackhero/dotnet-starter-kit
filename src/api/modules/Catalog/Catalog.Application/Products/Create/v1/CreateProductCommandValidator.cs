using FluentValidation;

namespace FSH.Starter.WebApi.Catalog.Application.Products.Create.v1;
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(p => p.Name).NotEmpty().MinimumLength(2).MaximumLength(75);
        RuleFor(p => p.Price).GreaterThan(0);
    }
}
