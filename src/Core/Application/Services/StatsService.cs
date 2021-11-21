using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DN.WebApi.Application.Abstractions.Repositories;
using DN.WebApi.Application.Abstractions.Services;
using DN.WebApi.Application.Abstractions.Services.Identity;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Entities.Catalog;
using DN.WebApi.Shared.DTOs;

namespace DN.WebApi.Application.Services
{
    public class StatsService : IStatsService
    {
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly IRepositoryAsync _repository;

        public StatsService(IRepositoryAsync repository, IRoleService roleService, IUserService userService)
        {
            _repository = repository;
            _roleService = roleService;
            _userService = userService;
        }

        public async Task<IResult<StatsDto>> GetDataAsync()
        {
            var stats = new StatsDto
            {
                ProductCount = await _repository.GetCountAsync<Product>(),
                BrandCount = await _repository.GetCountAsync<Brand>(),
                UserCount = await _userService.GetCountAsync(),
                RoleCount = await _roleService.GetCountAsync()
            };
            return await Result<StatsDto>.SuccessAsync(stats);
        }
    }
}