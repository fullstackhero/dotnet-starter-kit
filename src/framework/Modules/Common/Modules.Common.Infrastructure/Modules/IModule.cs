using FSH.Modules.Common.Core.Modules;
using Microsoft.AspNetCore.Builder;

namespace FSH.Modules.Common.Infrastructure.Modules;
public interface IModule : ICoreModule
{
    void ConfigureModule(WebApplication app);
}