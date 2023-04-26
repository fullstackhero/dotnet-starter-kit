using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Provinces;

public class CreateProvinceRequestValidator : CustomValidator<CreateProvinceRequest>
{
    public CreateProvinceRequestValidator(IReadRepository<Province> entityRepo, IStringLocalizer<CreateProvinceRequestValidator> t)
    {
        RuleFor(e => e.Code)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (code, ct) => await entityRepo.FirstOrDefaultAsync(new ProvinceByCodeSpec(code), ct) is null)
                .WithMessage((_, code) => t["Province with Code{0} already exists.", code]);

        RuleFor(e => e.Name)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (name, ct) => await entityRepo.FirstOrDefaultAsync(new ProvinceByNameSpec(name), ct) is null)
                .WithMessage((_, name) => t["Province with Name {0} already exists.", name]);

        RuleFor(e => e.StateId)
            .NotEmpty()
                .WithMessage(" The Country is required");
    }
}

public class UpdateProvinceRequestValidator : CustomValidator<UpdateProvinceRequest>
{
    public UpdateProvinceRequestValidator(IReadRepository<Province> entityRepo, IStringLocalizer<UpdateProvinceRequestValidator> t)
    {
        RuleFor(e => e.Code)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (entity, code, ct) =>
                    await entityRepo.FirstOrDefaultAsync(new ProvinceByCodeSpec(code), ct)
                        is not Province existingEntity || existingEntity.Id == entity.Id)
                .WithMessage((_, code) => t["Province {0} already Exists.", code]);

        RuleFor(e => e.Name)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (entity, name, ct) =>
                    await entityRepo.FirstOrDefaultAsync(new ProvinceByNameSpec(name), ct)
                        is not Province existingEntity || existingEntity.Id == entity.Id)
                .WithMessage((_, name) => t["Province {0} already Exists.", name]);

        RuleFor(e => e.StateId)
            .NotEmpty()
                .WithMessage(" The Country is required");
    }
}