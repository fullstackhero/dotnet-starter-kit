using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.GeoAdminUnits;

public class CreateGeoAdminUnitRequestValidator : CustomValidator<CreateGeoAdminUnitRequest>
{
    public CreateGeoAdminUnitRequestValidator(IReadRepository<GeoAdminUnit> entityRepo, IStringLocalizer<CreateGeoAdminUnitRequestValidator> T)
    {
        RuleFor(e => e.Code)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (code, ct) => await entityRepo.FirstOrDefaultAsync(new GeoAdminUnitByCodeSpec(code), ct) is null)
                .WithMessage((_, code) => T["Entity with Code{0} already exists.", code]);

        RuleFor(e => e.Name)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (name, ct) => await entityRepo.FirstOrDefaultAsync(new GeoAdminUnitByNameSpec(name), ct) is null)
                .WithMessage((_, name) => T["Entity with Name {0} already exists.", name]);
    }
}

public class UpdateGeoAdminUnitRequestValidator : CustomValidator<UpdateGeoAdminUnitRequest>
{
    public UpdateGeoAdminUnitRequestValidator(IReadRepository<GeoAdminUnit> entityRepo, IStringLocalizer<UpdateGeoAdminUnitRequestValidator> T)
    {
        RuleFor(e => e.Code)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (entity, code, ct) =>
                    await entityRepo.FirstOrDefaultAsync(new GeoAdminUnitByCodeSpec(code), ct)
                        is not GeoAdminUnit existingEntity || existingEntity.Id == entity.Id)
                .WithMessage((_, code) => T["Entity {0} already Exists.", code]);

        RuleFor(e => e.Name)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (entity, name, ct) =>
                    await entityRepo.FirstOrDefaultAsync(new GeoAdminUnitByNameSpec(name), ct)
                        is not GeoAdminUnit existingEntity || existingEntity.Id == entity.Id)
                .WithMessage((_, name) => T["Entity {0} already Exists.", name]);
    }
}