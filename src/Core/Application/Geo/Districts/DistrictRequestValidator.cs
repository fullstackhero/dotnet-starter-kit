using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Districts;

public class CreateDistrictRequestValidator : CustomValidator<CreateDistrictRequest>
{
    public CreateDistrictRequestValidator(IReadRepository<District> entityRepo, IStringLocalizer<CreateDistrictRequestValidator> t)
    {
        RuleFor(e => e.Code)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (code, ct) => await entityRepo.FirstOrDefaultAsync(new DistrictByCodeSpec(code), ct) is null)
                .WithMessage((_, code) => t["District with Code{0} already exists.", code]);

        RuleFor(e => e.Name)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (name, ct) => await entityRepo.FirstOrDefaultAsync(new DistrictByNameSpec(name), ct) is null)
                .WithMessage((_, name) => t["District with Name {0} already exists.", name]);

        RuleFor(e => e.ProvinceId)
            .NotEmpty()
                .WithMessage(" The Province is required");
    }
}

public class UpdateDistrictRequestValidator : CustomValidator<UpdateDistrictRequest>
{
    public UpdateDistrictRequestValidator(IReadRepository<District> entityRepo, IStringLocalizer<UpdateDistrictRequestValidator> t)
    {
        RuleFor(e => e.Code)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (entity, code, ct) =>
                    await entityRepo.FirstOrDefaultAsync(new DistrictByCodeSpec(code), ct)
                        is not District existingEntity || existingEntity.Id == entity.Id)
                .WithMessage((_, code) => t["District {0} already Exists.", code]);

        RuleFor(e => e.Name)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (entity, name, ct) =>
                    await entityRepo.FirstOrDefaultAsync(new DistrictByNameSpec(name), ct)
                        is not District existingEntity || existingEntity.Id == entity.Id)
                .WithMessage((_, name) => t["District {0} already Exists.", name]);

        RuleFor(e => e.ProvinceId)
            .NotEmpty()
                .WithMessage(" The Province is required");
    }
}