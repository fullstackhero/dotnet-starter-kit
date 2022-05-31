using System.Reflection;
using FSH.WebApi.Application.Common.Interfaces;
using FSH.WebApi.Domain.Catalog;
using FSH.WebApi.Domain.Dog;
using FSH.WebApi.Infrastructure.Persistence.Context;
using FSH.WebApi.Infrastructure.Persistence.Initialization;
using Microsoft.Extensions.Logging;

namespace FSH.WebApi.Infrastructure.Dsc.DogTraits;

public class DogGroupsSeeder : ICustomSeeder
{
    private readonly ISerializerService _serializerService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<DogGroupsSeeder> _logger;

    public DogGroupsSeeder(ISerializerService serializerService, ILogger<DogGroupsSeeder> logger, ApplicationDbContext db)
    {
        _serializerService = serializerService;
        _logger = logger;
        _db = db;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        string? path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (!_db.DogGroups.Any())
        {
            _logger.LogInformation("Started to seed Dog Groups.");

            // Here you can use your own logic to populate the database.
            // As an example, I am using a JSON file to populate the database.
            string dogGroupsData = await File.ReadAllTextAsync(path + "/Dsc/DogGroups/DogGroups.json", cancellationToken);
            var dogGroups = _serializerService.Deserialize<List<DogGroup>>(dogGroupsData);

            if (dogGroups != null)
            {
                foreach (var group in dogGroups)
                {
                    await _db.DogGroups.AddAsync(group, cancellationToken);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded Dog Groups.");
        }
    }
}