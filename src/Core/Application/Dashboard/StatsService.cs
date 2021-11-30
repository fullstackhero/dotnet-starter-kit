using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Identity.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Catalog;
using DN.WebApi.Shared.DTOs;

namespace DN.WebApi.Application.Dashboard;

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