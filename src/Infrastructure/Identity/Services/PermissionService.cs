using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Application.Abstractions.Services.Identity;
using DN.WebApi.Application.Constants;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Persistence;
using DN.WebApi.Shared.DTOs.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mapster;

namespace DN.WebApi.Infrastructure.Identity.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ICurrentUser _currentUser;
        private readonly ISerializerService _serializer;

        public PermissionService(ApplicationDbContext context, ICurrentUser currentUser, IDistributedCache cache, ISerializerService serializer)
        {
            _context = context;
            _currentUser = currentUser;
            _cache = cache;
            _serializer = serializer;
        }

        public async Task<bool> HasPermissionAsync(string userId, string permission)
        {
            var roles = new List<RoleDto>();
            var cacheKey = CacheKeys.GetCacheKey(ClaimConstants.Permission, userId);
            byte[] cachedData = !string.IsNullOrWhiteSpace(cacheKey) ? await _cache.GetAsync(cacheKey) : null;
            if (cachedData != null)
            {
                await _cache.RefreshAsync(cacheKey);
                roles = _serializer.Deserialize<List<RoleDto>>(Encoding.Default.GetString(cachedData));
            }
            else
            {
                var userRoles = await _context.UserRoles.Where(a => a.UserId == userId).Select(a => a.RoleId).ToListAsync();
                var applicationRoles = await _context.Roles.Where(a => userRoles.Contains(a.Id)).ToListAsync();
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
                if (_context.RoleClaims.Any(a => a.ClaimType == ClaimConstants.Permission && a.ClaimValue == permission && a.RoleId == role.Id))
                {
                    return true;
                }
            }

            return false;
        }
    }
}