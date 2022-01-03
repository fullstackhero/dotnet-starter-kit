using Ardalis.ApiEndpoints;
using DN.WebApi.Application.Catalog.Interfaces;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Host.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Host.Endpoints.Catalog.Brands;

public class DeleteRandom : EndpointBaseAsync
    .WithoutRequest
    .WithResult<Result<string>>
{
    private readonly IJobService _jobService;

    public DeleteRandom(IJobService jobService) => _jobService = jobService;

    [HttpDelete("delete-random")]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Delete))]
    public override Task<Result<string>> HandleAsync(CancellationToken cancellationToken = default)
    {
        string jobId = _jobService.Schedule<IBrandGeneratorJob>(x => x.CleanAsync(), TimeSpan.FromSeconds(5));
        return Result<string>.SuccessAsync(jobId);
    }
}