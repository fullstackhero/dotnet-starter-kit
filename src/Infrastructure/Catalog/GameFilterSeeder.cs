using System.Reflection;
using FSH.WebApi.Application.Common.Interfaces;
using FSH.WebApi.Domain.Catalog;
using FSH.WebApi.Infrastructure.Persistence.Context;
using FSH.WebApi.Infrastructure.Persistence.Initialization;
using Microsoft.Extensions.Logging;

namespace FSH.WebApi.Infrastructure.Catalog;

public class GameFilterSeeder : ICustomSeeder
{
    private readonly ISerializerService _serializerService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<GameFilterSeeder> _logger;

    public GameFilterSeeder(ISerializerService serializerService, ILogger<GameFilterSeeder> logger, ApplicationDbContext db)
    {
        _serializerService = serializerService;
        _logger = logger;
        _db = db;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        string? path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (!_db.GameFilters.Any())
        {
            _logger.LogInformation("Started to Seed game types.");

            // Here you can use your own logic to populate the database.
            // As an example, I am using a JSON file to populate the database.
            string brandData = await File.ReadAllTextAsync(path + "/Catalog/GameFilters.json", cancellationToken);
            var GameFilters = _serializerService.Deserialize<List<GameFilter>>(brandData);

            if (GameFilters != null)
            {
                foreach (var GameFilter in GameFilters)
                {
                    await _db.GameFilters.AddAsync(GameFilter, cancellationToken);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded game types.");
        }
    }
}