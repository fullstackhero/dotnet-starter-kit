using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs;

namespace DN.WebApi.Application.Abstractions.Services;

public interface IStatsService : ITransientService
{
    Task<IResult<StatsDto>> GetDataAsync();
}