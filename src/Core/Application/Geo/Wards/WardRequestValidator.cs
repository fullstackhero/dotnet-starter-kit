using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Wards;

public class CreateWardRequestValidator : CustomValidator<CreateWardRequest>
{
    public CreateWardRequestValidator(IReadRepository<Ward> entityRepo, IStringLocalizer<CreateWardRequestValidator> t)
    {
        RuleFor(e => e.Code)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (code, ct) => await entityRepo.FirstOrDefaultAsync(new WardByCodeSpec(code), ct) is null)
                .WithMessage((_, code) => t["Ward with Code{0} already exists.", code]);

        RuleFor(e => e.Name)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (name, ct) => await entityRepo.FirstOrDefaultAsync(new WardByNameSpec(name), ct) is null)
                .WithMessage((_, name) => t["Ward with Name {0} already exists.", name]);

        RuleFor(e => e.DistrictId)
            .NotEmpty()
                .WithMessage(" The Continent is required");
    }
}

public class UpdateWardRequestValidator : CustomValidator<UpdateWardRequest>
{
    public UpdateWardRequestValidator(IReadRepository<Ward> entityRepo, IStringLocalizer<UpdateWardRequestValidator> t)
    {
        RuleFor(e => e.Code)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (entity, code, ct) =>
                    await entityRepo.FirstOrDefaultAsync(new WardByCodeSpec(code), ct)
                        is not Ward existingEntity || existingEntity.Id == entity.Id)
                .WithMessage((_, code) => t["Ward {0} already Exists.", code]);

        RuleFor(e => e.Name)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (entity, name, ct) =>
                    await entityRepo.FirstOrDefaultAsync(new WardByNameSpec(name), ct)
                        is not Ward existingEntity || existingEntity.Id == entity.Id)
                .WithMessage((_, name) => t["Ward {0} already Exists.", name]);

        RuleFor(e => e.DistrictId)
            .NotEmpty()
                .WithMessage(" The Country is required");
    }
}