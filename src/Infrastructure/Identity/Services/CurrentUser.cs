using System;
using System.Collections.Generic;
using System.Security.Claims;
using DN.WebApi.Application.Abstractions.Services.Identity;
using DN.WebApi.Infrastructure.Extensions;
using Hangfire.Server;
using Microsoft.AspNetCore.Http;

namespace DN.WebApi.Infrastructure.Identity.Services
{
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly PerformingContext _performingContext;

        public CurrentUser(IHttpContextAccessor accessor, PerformingContext performingContext)
        {
            _accessor = accessor;
            _performingContext = performingContext;
        }

        public string Name => _accessor.HttpContext?.User.Identity?.Name;

        public Guid GetUserId()
        {
            if (IsAuthenticated())
            {
                return Guid.Parse(_accessor.HttpContext?.User.GetUserId() ?? Guid.Empty.ToString());
            }
            else if (_performingContext != null)
            {
                string userId = _performingContext.GetJobParameter<string>("userId");
                if (!string.IsNullOrEmpty(userId))
                {
                    return Guid.Parse(userId);
                }
            }

            return Guid.Empty;
        }

        public string GetUserEmail()
        {
            return IsAuthenticated() ? _accessor.HttpContext?.User.GetUserEmail() : string.Empty;
        }

        public bool IsAuthenticated()
        {
            return _accessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
        }

        public bool IsInRole(string role)
        {
            return _accessor.HttpContext?.User.IsInRole(role) ?? false;
        }

        public IEnumerable<Claim> GetUserClaims()
        {
            return _accessor.HttpContext?.User.Claims;
        }

        public HttpContext GetHttpContext()
        {
            return _accessor.HttpContext;
        }

        public string GetTenant()
        {
            if (IsAuthenticated())
            {
                return _accessor.HttpContext?.User.GetTenant();
            }
            else if (_performingContext != null)
            {
                string tenant = _performingContext.GetJobParameter<string>("tenant");
                if (!string.IsNullOrEmpty(tenant))
                {
                    return tenant;
                }
            }

            return string.Empty;
        }
    }
}