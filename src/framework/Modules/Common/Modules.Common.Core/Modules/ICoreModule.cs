using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Core.Modules;

public interface ICoreModule
{
    IServiceCollection AddModuleServices(IServiceCollection services, IConfiguration config);
}