using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Modules.Common.Core.Modules;

public interface ICoreModule
{
    void AddModule(IServiceCollection services, IConfiguration config);
}