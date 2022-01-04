using System.Reflection;
using DN.WebApi.Application.Common.Endpoints;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

[assembly: ApiConventionType(typeof(FSHApiEndpointConvention))]

namespace DN.WebApi.Application;

public static class Startup
{
    public static IServiceCollection AddApplication(this IServiceCollection services) =>
        services
            .AddValidatorsFromAssembly(Assembly.GetExecutingAssembly())
            .AddMediatR(Assembly.GetExecutingAssembly());
}