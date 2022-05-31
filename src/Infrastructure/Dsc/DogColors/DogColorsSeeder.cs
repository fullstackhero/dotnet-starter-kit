using System.Reflection;
using FSH.WebApi.Application.Common.Interfaces;
using FSH.WebApi.Domain.Dog;
using FSH.WebApi.Infrastructure.Persistence.Context;
using FSH.WebApi.Infrastructure.Persistence.Initialization;
using Microsoft.Extensions.Logging;

namespace FSH.WebApi.Infrastructure.Dsc.DogTraits;

public class DogColorsSeeder : ICustomSeeder
{
    private readonly ISerializerService _serializerService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<DogColorsSeeder> _logger;

    public DogColorsSeeder(ISerializerService serializerService, ILogger<DogColorsSeeder> logger, ApplicationDbContext db)
    {
        _serializerService = serializerService;
        _logger = logger;
        _db = db;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        string? path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (!_db.DogColors.Any())
        {
            _logger.LogInformation("Started to seed Dog Colors.");

            // Here you can use your own logic to populate the database.
            // As an example, I am using a JSON file to populate the database.
            string dogColorsData = await File.ReadAllTextAsync(path + "/Dsc/DogColors/DogColors.json", cancellationToken);
            var dogColors = _serializerService.Deserialize<List<DogColor>>(dogColorsData);

            if (dogColors != null)
            {
                foreach (var color in dogColors)
                {
                    await _db.DogColors.AddAsync(color, cancellationToken);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded Dog Colors.");
        }
    }
}