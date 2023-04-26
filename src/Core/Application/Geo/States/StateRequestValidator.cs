using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.States;

public class CreateStateRequestValidator : CustomValidator<CreateStateRequest>
{
    public CreateStateRequestValidator(IReadRepository<State> entityRepo, IStringLocalizer<CreateStateRequestValidator> t)
    {
        RuleFor(e => e.Code)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (code, ct) => await entityRepo.FirstOrDefaultAsync(new StateByCodeSpec(code), ct) is null)
                .WithMessage((_, code) => t["State with Code{0} already exists.", code]);

        RuleFor(e => e.Name)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (name, ct) => await entityRepo.FirstOrDefaultAsync(new StateByNameSpec(name), ct) is null)
                .WithMessage((_, name) => t["State with Name {0} already exists.", name]);

        RuleFor(e => e.CountryId)
            .NotEmpty()
                .WithMessage(" The Country is required");
    }
}

public class UpdateStateRequestValidator : CustomValidator<UpdateStateRequest>
{
    public UpdateStateRequestValidator(IReadRepository<State> entityRepo, IStringLocalizer<UpdateStateRequestValidator> t)
    {
        RuleFor(e => e.Code)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (entity, code, ct) =>
                    await entityRepo.FirstOrDefaultAsync(new StateByCodeSpec(code), ct)
                        is not State existingEntity || existingEntity.Id == entity.Id)
                .WithMessage((_, code) => t["State {0} already Exists.", code]);

        RuleFor(e => e.Name)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (entity, name, ct) =>
                    await entityRepo.FirstOrDefaultAsync(new StateByNameSpec(name), ct)
                        is not State existingEntity || existingEntity.Id == entity.Id)
                .WithMessage((_, name) => t["State {0} already Exists.", name]);

        RuleFor(e => e.CountryId)
            .NotEmpty()
                .WithMessage(" The Country is required");
    }
}