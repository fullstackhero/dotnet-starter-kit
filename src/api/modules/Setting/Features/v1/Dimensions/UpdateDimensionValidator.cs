using FluentValidation;
using FSH.Starter.WebApi.Setting.Persistence;

namespace FSH.Starter.WebApi.Setting.Features.v1.Dimensions;
public class UpdateDimensionValidator : AbstractValidator<UpdateDimensionCommand>
{
    public UpdateDimensionValidator(DimensionDbContext context)
    {
        RuleFor(p => p.Code).NotEmpty();
        RuleFor(p => p.Name).NotEmpty();
    }
}
