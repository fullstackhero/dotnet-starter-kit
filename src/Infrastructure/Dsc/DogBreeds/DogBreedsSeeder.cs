using System.Reflection;
using FSH.WebApi.Application.Common.Interfaces;
using FSH.WebApi.Domain.Dog;
using FSH.WebApi.Infrastructure.Persistence.Context;
using FSH.WebApi.Infrastructure.Persistence.Initialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.WebApi.Infrastructure.Dsc.DogBreeds;

public class DogBreedsSeeder : ICustomSeeder
{
    private readonly ISerializerService _serializerService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<DogBreedsSeeder> _logger;

    public DogBreedsSeeder(ISerializerService serializerService, ILogger<DogBreedsSeeder> logger, ApplicationDbContext db)
    {
        _serializerService = serializerService;
        _logger = logger;
        _db = db;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        string? path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (!_db.DogBreeds.Any())
        {
            _logger.LogInformation("Started seeding dog breeds.");

            // Here you can use your own logic to populate the database.
            // As an example, I am using a JSON file to populate the database.
            string dogBreedsData = await File.ReadAllTextAsync(path + "/Dsc/DogBreeds/DogBreeds.json", cancellationToken);
            var dogBreeds = _serializerService.Deserialize<List<DogBreed>>(dogBreedsData);
            if (dogBreeds != null)
            {
                foreach (var breed in dogBreeds)
                {
                    var dogbreed = await _db.DogBreeds.AddAsync(breed, cancellationToken);
                    if (dogbreed != null)
                    {
                        string dogColorsData = await File.ReadAllTextAsync(path + $"/Dsc/DogBreeds/{dogbreed.Entity.Name.Replace(" ", string.Empty)}Colors.json", cancellationToken);
                        foreach (var dogcolor in _serializerService.Deserialize<List<DogColor>>(dogColorsData))
                        {
                            if (dogbreed.Entity.Colors == null)
                            {
                                dogbreed.Entity.Colors = new();
                            }

                            var dbdogcolor = await _db.DogColors.Where(c => c.Name.Equals(dogcolor.Name)).FirstOrDefaultAsync(cancellationToken: cancellationToken);

                            if (dbdogcolor != null)
                                dogbreed.Entity.Colors.Add(dbdogcolor);
                        }

                        string dogTraitsData = await File.ReadAllTextAsync(path + $"/Dsc/DogBreeds/{dogbreed.Entity.Name.Replace(" ", string.Empty)}Traits.json", cancellationToken);
                        foreach (var dogtrait in _serializerService.Deserialize<List<DogTrait>>(dogTraitsData))
                        {
                            if (dogbreed.Entity.Traits == null)
                            {
                                dogbreed.Entity.Traits = new();
                            }

                            var dbdogtrait = await _db.DogTraits.Where(c => c.Name.Equals(dogtrait.Name)).FirstOrDefaultAsync(cancellationToken: cancellationToken);

                            if (dbdogtrait != null)
                                dogbreed.Entity.Traits.Add(dbdogtrait);
                        }

                        string dogGroupData = await File.ReadAllTextAsync(path + $"/Dsc/DogBreeds/{dogbreed.Entity.Name.Replace(" ", string.Empty)}Group.json", cancellationToken);
                        var doggroup = _serializerService.Deserialize<DogGroup>(dogGroupData);

                        var dbdogGroup = await _db.DogGroups.Where(c => c.Name.Equals(doggroup.Name)).FirstOrDefaultAsync(cancellationToken: cancellationToken);

                        if(dbdogGroup != null)
                            dogbreed.Entity.Group = dbdogGroup;

                    }
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded dog breeds.");
        }
    }
}