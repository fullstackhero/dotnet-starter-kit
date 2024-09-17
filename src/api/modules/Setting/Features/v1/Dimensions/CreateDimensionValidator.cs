using FluentValidation;
using FSH.Starter.WebApi.Setting.Persistence;

namespace FSH.Starter.WebApi.Setting.Features.v1.Dimensions;
public class CreateDimensionValidator : AbstractValidator<CreateDimensionCommand>
{
    public CreateDimensionValidator(SettingDbContext context)
    {
        RuleFor(p => p.Code).NotEmpty();
        RuleFor(p => p.Name).NotEmpty();
    }
}
