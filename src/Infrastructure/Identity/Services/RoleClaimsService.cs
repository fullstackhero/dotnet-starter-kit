using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Identity.Models;
using DN.WebApi.Infrastructure.Persistence.Contexts;
using DN.WebApi.Shared.DTOs.Identity;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using System.Text;
using DN.WebApi.Application.Common.Constants;
using DN.WebApi.Application.Identity.Interfaces;

namespace DN.WebApi.Infrastructure.Identity.Services;

public class RoleClaimsService : IRoleClaimsService
{
    private readonly ApplicationDbContext _db;
    private readonly ICacheService _cache;
    private readonly ISerializerService _serializer;
    private readonly IStringLocalizer<RoleClaimsService> _localizer;

    public RoleClaimsService(ApplicationDbContext context, ICacheService cache, ISerializerService serializer, IStringLocalizer<RoleClaimsService> localizer)
    {
        _db = context;
        _cache = cache;
        _serializer = serializer;
        _localizer = localizer;
    }

    public async Task<bool> HasPermissionAsync(string userId, string permission)
    {
        var roles = new List<RoleDto>();
        string cacheKey = CacheKeys.GetCacheKey(ClaimConstants.Permission, userId);
        byte[] cachedData = !string.IsNullOrWhiteSpace(cacheKey) ? await _cache.GetAsync(cacheKey) : null;
        if (cachedData != null)
        {
            await _cache.RefreshAsync(cacheKey);
            roles = _serializer.Deserialize<List<RoleDto>>(Encoding.Default.GetString(cachedData));
        }
        else
        {
            var userRoles = await _db.UserRoles.Where(a => a.UserId == userId).Select(a => a.RoleId).ToListAsync();
            var applicationRoles = await _db.Roles.Where(a => userRoles.Contains(a.Id)).ToListAsync();
            roles = applicationRoles.Adapt<List<RoleDto>>();
            if (roles != null)
            {
                var options = new DistributedCacheEntryOptions();
                byte[] serializedData = Encoding.Default.GetBytes(_serializer.Serialize(roles));
                await _cache.SetAsync(cacheKey, serializedData, options);
            }
        }

        if (roles == null) return false;
        foreach (var role in roles)
        {
            if (_db.RoleClaims.Any(a => a.ClaimType == ClaimConstants.Permission && a.ClaimValue == permission && a.RoleId == role.Id))
            {
                return true;
            }
        }

        return false;
    }

    public async Task<Result<List<RoleClaimResponse>>> GetAllAsync()
    {
        var roleClaims = await _db.RoleClaims.ToListAsync();
        var roleClaimsResponse = roleClaims.Adapt<List<RoleClaimResponse>>();
        return await Result<List<RoleClaimResponse>>.SuccessAsync(roleClaimsResponse);
    }

    public async Task<int> GetCountAsync()
    {
        return await _db.RoleClaims.CountAsync();
    }

    public async Task<Result<RoleClaimResponse>> GetByIdAsync(int id)
    {
        var roleClaim = await _db.RoleClaims
            .SingleOrDefaultAsync(x => x.Id == id);
        var roleClaimResponse = roleClaim.Adapt<RoleClaimResponse>();
        return await Result<RoleClaimResponse>.SuccessAsync(roleClaimResponse);
    }

    public async Task<Result<List<RoleClaimResponse>>> GetAllByRoleIdAsync(string roleId)
    {
        var roleClaims = await _db.RoleClaims
            .Include(x => x.Role)
            .Where(x => x.RoleId == roleId)
            .ToListAsync();
        var roleClaimsResponse = roleClaims.Adapt<List<RoleClaimResponse>>();
        return await Result<List<RoleClaimResponse>>.SuccessAsync(roleClaimsResponse);
    }

    public async Task<Result<string>> SaveAsync(RoleClaimRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RoleId))
        {
            return await Result<string>.FailAsync(_localizer["Role is required."]);
        }

        if (request.Id == 0)
        {
            var existingRoleClaim =
                await _db.RoleClaims
                    .SingleOrDefaultAsync(x =>
                        x.RoleId == request.RoleId && x.ClaimType == request.Type && x.ClaimValue == request.Value);
            if (existingRoleClaim != null)
            {
                return await Result<string>.FailAsync(_localizer["Similar Role Claim already exists."]);
            }

            var roleClaim = request.Adapt<ApplicationRoleClaim>();
            await _db.RoleClaims.AddAsync(roleClaim);
            await _db.SaveChangesAsync();
            return await Result<string>.SuccessAsync(string.Format(_localizer["Role Claim {0} created."], request.Value));
        }
        else
        {
            var existingRoleClaim =
                await _db.RoleClaims
                    .Include(x => x.Role)
                    .SingleOrDefaultAsync(x => x.Id == request.Id);
            if (existingRoleClaim == null)
            {
                return await Result<string>.SuccessAsync(_localizer["Role Claim does not exist."]);
            }
            else
            {
                existingRoleClaim.ClaimType = request.Type;
                existingRoleClaim.ClaimValue = request.Value;
                existingRoleClaim.Group = request.Group;
                existingRoleClaim.Description = request.Description;
                existingRoleClaim.RoleId = request.RoleId;
                _db.RoleClaims.Update(existingRoleClaim);
                await _db.SaveChangesAsync();
                return await Result<string>.SuccessAsync(string.Format(_localizer["Role Claim {0} for Role {1} updated."], request.Value, existingRoleClaim.Role.Name));
            }
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        var existingRoleClaim = await _db.RoleClaims
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (existingRoleClaim != null)
        {
            _db.RoleClaims.Remove(existingRoleClaim);
            await _db.SaveChangesAsync();
            return await Result<string>.SuccessAsync(string.Format(_localizer["Role Claim {0} for {1} Role deleted."], existingRoleClaim.ClaimValue, existingRoleClaim.Role.Name));
        }
        else
        {
            return await Result<string>.FailAsync(_localizer["Role Claim does not exist."]);
        }
    }
}