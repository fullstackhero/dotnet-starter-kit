using FluentValidation;

namespace FSH.Starter.WebApi.Catalog.Application.Products.Update.v1;
public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(p => p.Name).NotEmpty().MinimumLength(2).MaximumLength(75);
        RuleFor(p => p.Price).GreaterThan(0);
    }
}
