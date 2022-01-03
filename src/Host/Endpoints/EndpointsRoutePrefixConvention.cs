using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace DN.WebApi.Host.Endpoints;

public static class MvcOptionsExtensions
{
    public static void UseEndpointsConvention(this MvcOptions opts, string? routePrefix = null) =>
        opts.Conventions.Add(new EndpointsRoutePrefixConvention(routePrefix));
}

public class EndpointsRoutePrefixConvention : IApplicationModelConvention
{
    private const string DefaultRoutePrefix = "api/v{version:apiVersion}/[namespace]";

    private readonly AttributeRouteModel _routeModel;

    public EndpointsRoutePrefixConvention(string? routePrefix = null) =>
        _routeModel = new AttributeRouteModel(
            new RouteAttribute(routePrefix ?? DefaultRoutePrefix));

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