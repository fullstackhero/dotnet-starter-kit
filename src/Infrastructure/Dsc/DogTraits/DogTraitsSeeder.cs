using System.Reflection;
using FSH.WebApi.Application.Common.Interfaces;
using FSH.WebApi.Domain.Catalog;
using FSH.WebApi.Domain.Dog;
using FSH.WebApi.Infrastructure.Persistence.Context;
using FSH.WebApi.Infrastructure.Persistence.Initialization;
using Microsoft.Extensions.Logging;

namespace FSH.WebApi.Infrastructure.Dsc.DogTraits;

public class DogTraitsSeeder : ICustomSeeder
{
    private readonly ISerializerService _serializerService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<DogTraitsSeeder> _logger;

    public DogTraitsSeeder(ISerializerService serializerService, ILogger<DogTraitsSeeder> logger, ApplicationDbContext db)
    {
        _serializerService = serializerService;
        _logger = logger;
        _db = db;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        string? path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (!_db.DogTraits.Any())
        {
            _logger.LogInformation("Started seeding Dog Traits.");

            // Here you can use your own logic to populate the database.
            // As an example, I am using a JSON file to populate the database.
            string dogTraitsData = await File.ReadAllTextAsync(path + "/Dsc/DogTraits/DogTraits.json", cancellationToken);
            var dogTraits = _serializerService.Deserialize<List<DogTrait>>(dogTraitsData);

            if (dogTraits != null)
            {
                foreach (var trait in dogTraits)
                {
                    await _db.DogTraits.AddAsync(trait, cancellationToken);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded Dog Traits.");
        }
    }
}