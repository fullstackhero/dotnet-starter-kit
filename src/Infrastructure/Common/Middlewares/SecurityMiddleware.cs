using DN.WebApi.Application.Identity.Exceptions;
using DN.WebApi.Application.Identity.Interfaces;
using DN.WebApi.Application.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net;

namespace DN.WebApi.Infrastructure.Middlewares;

public class SecurityMiddleware : IMiddleware
{
    private static readonly ConcurrentDictionary<string, RequestFailed> _blackListIp = new();

    private readonly ICurrentUser _currentUser;
    private readonly MiddlewareSettings _settings;
    private DateTime _nextClear;

    public SecurityMiddleware(IOptions<MiddlewareSettings> midsettings, ICurrentUser currentUser)
    {
        _settings = midsettings.Value;
        _currentUser = currentUser;
        _nextClear = DateTime.UtcNow.AddMinutes(_settings.BlackListMinutes);
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (_nextClear <= DateTime.UtcNow)
        {
            _nextClear = DateTime.UtcNow.AddHours(1);
            foreach (var target in _blackListIp.ToArray())
            {
                if (target.Value.NextReset <= DateTime.UtcNow)
                    _blackListIp.TryRemove(target.Key, out _);
            }
        }

        // avoid to block auth users
        if (!_currentUser.IsAuthenticated())
        {
            // avoid x-forwarded tag, cracker can put fake input to avoid block
            string currentIp = context.Connection.RemoteIpAddress.MapToIPv4().ToString();
            if (_blackListIp.TryGetValue(currentIp, out var requestFailed))
            {
                if (requestFailed.Failded >= _settings.MaxAuthFailed && requestFailed.NextReset >= DateTime.UtcNow)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    await context.Response.WriteAsync("AuthBan");
                    return;
                }

                if (requestFailed.NextReset <= DateTime.UtcNow)
                    _blackListIp.TryRemove(currentIp, out _);
            }
        }

        await next(context);
    }

    public void UpdateBlockIp(IPAddress ipAddress)
    {
        UpdateBlockIp(ipAddress.MapToIPv4().ToString());
    }

    public void UpdateBlockIp(string ipAddress)
    {
        if (_blackListIp.TryGetValue(ipAddress, out var requestFailed))
        {
            if (requestFailed.Failded < _settings.MaxAuthFailed) //avoid overflow
                requestFailed.Failded++;
            requestFailed.NextReset = DateTime.UtcNow.AddMinutes(_settings.BlackListMinutes);
        }
        else
        {
            _blackListIp.TryAdd(ipAddress, new RequestFailed { Failded = 1, NextReset = DateTime.UtcNow.AddMinutes(_settings.BlackListMinutes) });
        }
    }

    public IEnumerable<string> GetBannedIps()
    {
        return _blackListIp.ToList().Where(i => i.Value.Failded >= _settings.MaxAuthFailed && i.Value.NextReset >= DateTime.UtcNow).Select(i => i.Key);
    }

    public bool UnBanIp(string ipTarget)
    {
        if (string.IsNullOrEmpty(ipTarget))
            return false;

        return _blackListIp.TryRemove(ipTarget, out _);
    }

    private class RequestFailed
    {
        public ushort Failded { get; set; }
        public DateTime NextReset { get; set; }
    }
}
