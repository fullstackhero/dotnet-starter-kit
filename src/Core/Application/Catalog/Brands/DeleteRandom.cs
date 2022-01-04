using Ardalis.ApiEndpoints;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Wrapper;
using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Application.Catalog.Brands;

public class DeleteRandom : EndpointBaseAsync
    .WithoutRequest
    .WithResult<Result<string>>
{
    private readonly IJobService _jobService;

    public DeleteRandom(IJobService jobService) => _jobService = jobService;

    [HttpDelete("delete-random")]
    public override Task<Result<string>> HandleAsync(CancellationToken cancellationToken = default)
    {
        string jobId = _jobService.Schedule<IBrandGeneratorJob>(x => x.CleanAsync(), TimeSpan.FromSeconds(5));
        return Result<string>.SuccessAsync(jobId);
    }
}