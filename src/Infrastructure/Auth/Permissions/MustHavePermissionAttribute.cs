using FSH.WebApi.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Configuration;

namespace FSH.WebApi.Infrastructure.Auth.Permissions;

public class MyApplicationModelProvider : IApplicationModelProvider
{
    private IConfiguration Config { get; set; }

    // constructor injection
    public MyApplicationModelProvider(IConfiguration config) {
        Config = config;
    }

    public int Order { get { return -1000 + 10; } }

    public void OnProvidersExecuted(ApplicationModelProviderContext context) {
        foreach (var controllerModel in context.Result.Controllers) {
            // pass the depencency to controller attibutes
            controllerModel.Attributes
                .OfType<MustHavePermissionAttribute>().ToList()
                .ForEach(a => a.Config = Config);

            // pass the dependency to action attributes
            controllerModel.Actions.SelectMany(a => a.Attributes)
                .OfType<MustHavePermissionAttribute>().ToList()
                .ForEach(a => a.Config = Config);
        }
    }

    public void OnProvidersExecuting(ApplicationModelProviderContext context) { // intentionally empty
    }
}

public class MustHavePermissionAttribute : AuthorizeAttribute {

    public IConfiguration Config { get; set; }

    public MustHavePermissionAttribute(string action, string resource) {
        if (Config != null) {
            if (Config.GetSection("FeatureFlagSettings").GetSection("Auth").Value == "True") {
                Policy = FSHPermission.NameFor(action, resource);
            }
        }
    }
}