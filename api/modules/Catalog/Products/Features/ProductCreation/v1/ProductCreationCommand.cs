using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace FSH.WebApi.Modules.Catalog.Products.Features.ProductCreation.v1;
public sealed record ProductCreationCommand(string? Name, decimal Price, string? Description = null) : IRequest<IResult>;

public class ProductCreationValidator : AbstractValidator<ProductCreationCommand>
{
    public ProductCreationValidator()
    {
        RuleFor(p => p.Name).NotEmpty().MinimumLength(10).MaximumLength(75);
        RuleFor(p => p.Price).GreaterThan(0);
    }
}
