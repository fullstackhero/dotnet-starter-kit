global using Ardalis.Specification;
global using DN.WebApi.Application.Catalog.Brands;
global using DN.WebApi.Application.Catalog.Products;
global using DN.WebApi.Application.Common.Events;
global using DN.WebApi.Application.Common.Exceptions;
global using DN.WebApi.Application.Common.FileStorage;
global using DN.WebApi.Application.Common.Interfaces;
global using DN.WebApi.Application.Common.Models;
global using DN.WebApi.Application.Common.Persistence;
global using DN.WebApi.Application.Common.Specification;
global using DN.WebApi.Application.Common.Validation;
global using DN.WebApi.Application.Identity.Roles;
global using DN.WebApi.Application.Identity.Users;
global using DN.WebApi.Domain.Catalog.Brands;
global using DN.WebApi.Domain.Catalog.Products;
global using DN.WebApi.Domain.Common;
global using DN.WebApi.Domain.Common.Contracts;
global using DN.WebApi.Domain.Multitenancy;
global using DN.WebApi.Shared.Notifications;
global using FluentValidation;
global using MediatR;
global using Microsoft.Extensions.Localization;
global using Microsoft.Extensions.Logging;

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace DN.WebApi.Application;

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