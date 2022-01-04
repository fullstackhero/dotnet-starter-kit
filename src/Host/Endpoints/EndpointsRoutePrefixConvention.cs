using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace DN.WebApi.Host.Endpoints;

public static class MvcOptionsExtensions
{
    private const string DefaultRoutePrefix = "api/v{version:apiVersion}/[namespace]";

    public static MvcOptions UseEndpointsRoutePrefixConvention(this MvcOptions options, string? routePrefix = null)
    {
        options.Conventions.Add(new EndpointsRoutePrefixConvention(routePrefix ?? DefaultRoutePrefix));
        return options;
    }
}

public class EndpointsRoutePrefixConvention : IApplicationModelConvention
{

    private readonly AttributeRouteModel _routeModel;

    public EndpointsRoutePrefixConvention(string routePrefix) =>
        _routeModel = new AttributeRouteModel(new RouteAttribute(routePrefix));

    public void Apply(ApplicationModel application)
    {
        foreach (var selector in application.Controllers
            .Where(controller => typeof(EndpointBase).IsAssignableFrom(controller.ControllerType))
            .SelectMany(controller => controller.Actions.SelectMany(a => a.Selectors)))
        {
            selector.AttributeRouteModel =
                selector.AttributeRouteModel is not null
                    ? AttributeRouteModel.CombineAttributeRouteModel(_routeModel, selector.AttributeRouteModel)
                    : _routeModel;
        }
    }
}