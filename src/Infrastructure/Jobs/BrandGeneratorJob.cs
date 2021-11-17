using DN.WebApi.Application.Abstractions.Jobs;
using DN.WebApi.Application.Abstractions.Repositories;
using DN.WebApi.Domain.Entities.Catalog;
using Hangfire;
using Hangfire.Console.Extensions;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace DN.WebApi.Infrastructure.Tasks
{
    public class BrandGeneratorJob : IBrandGeneratorJob
    {
        private readonly ILogger<BrandGeneratorJob> _logger;
        private readonly IRepositoryAsync _repository;
        private readonly IProgressBarFactory _progressBar;
        private readonly PerformingContext _performingContext;

        public BrandGeneratorJob(ILogger<BrandGeneratorJob> logger, IRepositoryAsync repository, IProgressBarFactory progressBar, PerformingContext performingContext)
        {
            _logger = logger;
            _repository = repository;
            _progressBar = progressBar;
            _performingContext = performingContext;
        }

        [Queue("notdefault")]
        [DisplayName("Generate Random Brand example job on Queue notDefault")]
        public async Task GenerateAsync(int nSeed)
        {
            // Example ProgressBar Hangfire
            var progress = _progressBar.Create();
            foreach (int index in Enumerable.Range(1, nSeed))
            {
                await _repository.CreateAsync<Brand>(new Brand(name: $"Brand Random - {Guid.NewGuid()}", "Funny description"));
                progress.SetValue(index * 100 / nSeed);
            }

            await _repository.SaveChangesAsync();
        }

        [Queue("notdefault")]
        [AutomaticRetry(Attempts = 5)]
        [DisplayName("removes all radom brands created example job on Queue notDefault")]
        public async Task CleanAsync()
        {
            _logger.LogInformation("Iniciando JobId: {JobId}", _performingContext.BackgroundJob.Id);

            // Example Logs to Serilog and HangFire
            _logger.LogTrace("Test - LogTrace");
            _logger.LogDebug("Test - LogDebug");
            _logger.LogInformation("Test - LogInformation");
            _logger.LogWarning("Test - LogWarning");
            _logger.LogError("Test - LogError");
            _logger.LogCritical("Test - LogCritical");

            var items = await _repository.GetListAsync<Brand>(x => x.Name.Contains("Brand Random"), true);
            _logger.LogInformation("Brands Random: {BrandsCount} ", items.Count.ToString());

            foreach (var item in items)
            {
                await _repository.RemoveAsync<Brand>(item);
            }

            int rows = await _repository.SaveChangesAsync();
            _logger.LogInformation("Rows affected: {rows} ", rows.ToString());
        }
    }
}
