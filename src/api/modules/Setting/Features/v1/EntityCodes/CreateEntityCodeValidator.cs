using FluentValidation;
using FSH.Starter.WebApi.Setting.Persistence;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;
public class CreateEntityCodeValidator : AbstractValidator<CreateEntityCodeCommand>
{
    public CreateEntityCodeValidator(EntityCodeDbContext context)
    {
        RuleFor(p => p.Code).NotEmpty();
        RuleFor(p => p.Name).NotEmpty();
    }
}
