using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.Blazor.Infrastructure.Auth.Jwt;

public class AccessTokenProviderAccessor : IAccessTokenProviderAccessor
{
    private readonly IServiceProvider _provider;
    private IAccessTokenProvider? _tokenProvider;

    public AccessTokenProviderAccessor(IServiceProvider provider) =>
        _provider = provider;

    public IAccessTokenProvider TokenProvider =>
        _tokenProvider ??= _provider.GetRequiredService<IAccessTokenProvider>();
}