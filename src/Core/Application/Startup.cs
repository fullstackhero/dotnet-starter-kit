global using Ardalis.Specification;
global using FluentValidation;
global using FSH.WebAPI.Application.Common.Events;
global using FSH.WebAPI.Application.Common.Exceptions;
global using FSH.WebAPI.Application.Common.FileStorage;
global using FSH.WebAPI.Application.Common.Interfaces;
global using FSH.WebAPI.Application.Common.Models;
global using FSH.WebAPI.Application.Common.Persistence;
global using FSH.WebAPI.Application.Common.Specification;
global using FSH.WebAPI.Application.Common.Validation;
global using FSH.WebAPI.Domain.Catalog.Brands;
global using FSH.WebAPI.Domain.Catalog.Products;
global using FSH.WebAPI.Domain.Common;
global using FSH.WebAPI.Domain.Common.Contracts;
global using FSH.WebAPI.Domain.Multitenancy;
global using FSH.WebAPI.Shared.Notifications;
global using MediatR;
global using Microsoft.Extensions.Localization;
global using Microsoft.Extensions.Logging;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.WebAPI.Application;

public static class Startup
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        return services
            .AddValidatorsFromAssembly(assembly)
            .AddMediatR(assembly);
    }
}