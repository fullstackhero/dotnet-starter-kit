using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs;

namespace DN.WebApi.Application.Dashboard;

public interface IStatsService : ITransientService
{
    Task<IResult<StatsDto>> GetDataAsync();
}