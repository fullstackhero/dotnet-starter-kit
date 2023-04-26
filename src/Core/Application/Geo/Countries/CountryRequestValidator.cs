using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Countries;

public class CreateCountryRequestValidator : CustomValidator<CreateCountryRequest>
{
    public CreateCountryRequestValidator(IReadRepository<Country> entityRepo, IStringLocalizer<CreateCountryRequestValidator> t)
    {
        RuleFor(e => e.Code)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (code, ct) => await entityRepo.FirstOrDefaultAsync(new CountryByCodeSpec(code), ct) is null)
                .WithMessage((_, code) => t["Country with Code{0} already exists.", code]);

        RuleFor(e => e.Name)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (name, ct) => await entityRepo.FirstOrDefaultAsync(new CountryByNameSpec(name), ct) is null)
                .WithMessage((_, name) => t["Country with Name {0} already exists.", name]);

        RuleFor(e => e.ContinentId)
            .NotEmpty()
                .WithMessage(" The Continent is required");

        RuleFor(e => e.TypeId)
            .NotEmpty()
                .WithMessage(" The Type is required");
    }
}

public class UpdateCountryRequestValidator : CustomValidator<UpdateCountryRequest>
{
    public UpdateCountryRequestValidator(IReadRepository<Country> entityRepo, IStringLocalizer<UpdateCountryRequestValidator> t)
    {
        RuleFor(e => e.Code)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (entity, code, ct) =>
                    await entityRepo.FirstOrDefaultAsync(new CountryByCodeSpec(code), ct)
                        is not Country existingEntity || existingEntity.Id == entity.Id)
                .WithMessage((_, code) => t["Country {0} already Exists.", code]);

        RuleFor(e => e.Name)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (entity, name, ct) =>
                    await entityRepo.FirstOrDefaultAsync(new CountryByNameSpec(name), ct)
                        is not Country existingEntity || existingEntity.Id == entity.Id)
                .WithMessage((_, name) => t["Country {0} already Exists.", name]);

        RuleFor(e => e.ContinentId)
            .NotEmpty()
                .WithMessage(" The Continent is required");

        RuleFor(e => e.TypeId)
            .NotEmpty()
                .WithMessage(" The Type is required");
    }
}