using System;
using System.Linq;
using System.Threading.Tasks;
using DN.WebApi.Application.Abstractions.Jobs;
using DN.WebApi.Application.Abstractions.Repositories;
using DN.WebApi.Domain.Entities.Catalog;
using Hangfire;
using Hangfire.Console.Extensions;
using Hangfire.Server;
using Microsoft.Extensions.Logging;

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
        public async Task GenerateAsync(int nSeed)
        {
            // Example ProgressBar Hangfire
            var progress = _progressBar.Create();
            foreach (int index in Enumerable.Range(1, nSeed))
            {
                await _repository.CreateAsync(new Brand(name: $"Brand Random - {Guid.NewGuid()}", "Funny description"));
                progress.SetValue(index * 100 / nSeed);
            }

            await _repository.SaveChangesAsync();
        }

        [Queue("notdefault")]
        [AutomaticRetry(Attempts = 5)]
        public async Task CleanAsync()
        {
            _logger.LogInformation("Initializing Job with Id: {JobId}", _performingContext.BackgroundJob.Id);
            var items = await _repository.GetListAsync<Brand>(x => x.Name.Contains("Brand Random"), true);
            _logger.LogInformation("Brands Random: {BrandsCount} ", items.Count.ToString());

            foreach (var item in items)
            {
                await _repository.RemoveAsync(item);
            }

            int rows = await _repository.SaveChangesAsync();
            _logger.LogInformation("Rows affected: {rows} ", rows.ToString());
        }
    }
}
