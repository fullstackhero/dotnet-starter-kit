using FSH.WebApi.Domain.Catalog.ChargeAggregate;
using Marten;
using Marten.Events;
using Marten.Events.Daemon.Resiliency;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Weasel.Core;

namespace FSH.WebApi.Infrastructure.EventSourcing;

public static class Startup
{
    public static IServiceCollection RegisterMarten(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Marten")
            ?? throw new ArgumentNullException("Marten connection string is missing");

        services.AddMarten(options =>
        {
            options.Connection(connectionString);
            options.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
            options.DatabaseSchemaName = EventSourcingConstants.SchemaName; // document store schema
            options.Events.DatabaseSchemaName = EventSourcingConstants.SchemaName; // event store schema
            options.Events.StreamIdentity = StreamIdentity.AsGuid;

            // options.Events.AsyncProjections.AggregateStreamsWith<Charge>();
            // options.Events.AsyncProjections.Add(new ChargeProjection());
        })
            .UseLightweightSessions()
            .AddAsyncDaemon(DaemonMode.Solo); // for async projections

        return services;
    }
}