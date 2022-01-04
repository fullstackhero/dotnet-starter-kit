using Ardalis.ApiEndpoints;
using DN.WebApi.Application.Catalog.Interfaces;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Wrapper;
using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Host.Endpoints.Catalog.Brands;

public class GenerateRandom : EndpointBaseSync
    .WithRequest<GenerateRandomBrandRequest>
    .WithResult<Result<string>>
{
    private readonly IJobService _jobService;

    public GenerateRandom(IJobService jobService) => _jobService = jobService;

    [HttpPost("generate-random")]
    public override Result<string> Handle(GenerateRandomBrandRequest request)
    {
        string jobId = _jobService.Enqueue<IBrandGeneratorJob>(x => x.GenerateAsync(request.NSeed));
        return Result<string>.Success(jobId);
    }
}