using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs;

namespace DN.WebApi.Application.Abstractions.Services
{
    public interface IStatsService : ITransientService
    {
        Task<IResult<StatsDto>> GetDataAsync();
    }
}