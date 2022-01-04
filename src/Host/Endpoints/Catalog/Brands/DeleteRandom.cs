using Ardalis.ApiEndpoints;
using DN.WebApi.Application.Catalog.Interfaces;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Wrapper;
using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Host.Endpoints.Catalog.Brands;

public class DeleteRandom : EndpointBaseSync
    .WithoutRequest
    .WithResult<Result<string>>
{
    private readonly IJobService _jobService;

    public DeleteRandom(IJobService jobService) => _jobService = jobService;

    [HttpDelete("delete-random")]
    public override Result<string> Handle()
    {
        string jobId = _jobService.Schedule<IBrandGeneratorJob>(x => x.CleanAsync(), TimeSpan.FromSeconds(5));
        return Result<string>.Success(jobId);
    }
}