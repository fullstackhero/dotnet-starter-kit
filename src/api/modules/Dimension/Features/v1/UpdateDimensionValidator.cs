using FluentValidation;
using FSH.Starter.WebApi.Setting.Dimension.Persistence;

namespace FSH.Starter.WebApi.Setting.Dimension.Features.v1;
public class UpdateDimensionValidator : AbstractValidator<UpdateDimensionCommand>
{
    public UpdateDimensionValidator(DimensionDbContext context)
    {
        RuleFor(p => p.Code).NotEmpty();
        RuleFor(p => p.Name).NotEmpty();
    }
}
