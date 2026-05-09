using Carter;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Infrastructure.Persistence;
using FSH.Starter.WebApi.Water.Domain;
using FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;
using FSH.Starter.WebApi.Water.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Water.Infrastructure;

public static class WaterModule
{
    public class Endpoints : CarterModule
    {
        public Endpoints() : base("water") { }
        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            var customerGroup = app.MapGroup("customers").WithTags("customers");
            customerGroup.MapCustomerCreationEndpoint();
            customerGroup.MapGetCustomerEndpoint();
            customerGroup.MapGetCustomerListEndpoint();
            customerGroup.MapCustomerUpdateEndpoint();
            customerGroup.MapCustomerDeleteEndpoint();

            var meterGroup = app.MapGroup("meters").WithTags("meters");
            meterGroup.MapMeterCreationEndpoint();
            meterGroup.MapGetMeterEndpoint();
            meterGroup.MapGetMeterListEndpoint();
            meterGroup.MapMeterUpdateEndpoint();
            meterGroup.MapMeterDeleteEndpoint();

            var meterReadingGroup = app.MapGroup("meter-readings").WithTags("meter-readings");
            meterReadingGroup.MapMeterReadingCreationEndpoint();
            meterReadingGroup.MapGetMeterReadingEndpoint();
            meterReadingGroup.MapGetMeterReadingListEndpoint();

            var tariffGroup = app.MapGroup("tariffs").WithTags("tariffs");
            tariffGroup.MapTariffCreationEndpoint();
            tariffGroup.MapGetTariffEndpoint();
            tariffGroup.MapGetTariffListEndpoint();
            tariffGroup.MapTariffUpdateEndpoint();
            tariffGroup.MapTariffDeleteEndpoint();

            var billGroup = app.MapGroup("bills").WithTags("bills");
            billGroup.MapBillCreationEndpoint();
            billGroup.MapGetBillEndpoint();
            billGroup.MapGetBillListEndpoint();

            var paymentGroup = app.MapGroup("payments").WithTags("payments");
            paymentGroup.MapPaymentCreationEndpoint();
            paymentGroup.MapGetPaymentEndpoint();
            paymentGroup.MapGetPaymentListEndpoint();

            var ticketGroup = app.MapGroup("trouble-tickets").WithTags("trouble-tickets");
            ticketGroup.MapMeterTroubleTicketCreationEndpoint();
            ticketGroup.MapGetMeterTroubleTicketEndpoint();
            ticketGroup.MapGetMeterTroubleTicketListEndpoint();
            ticketGroup.MapMeterTroubleTicketUpdateEndpoint();
            ticketGroup.MapMeterTroubleTicketDeleteEndpoint();
        }
    }

    public static WebApplicationBuilder RegisterWaterServices(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.BindDbContext<WaterDbContext>();
        builder.Services.AddScoped<IDbInitializer, WaterDbInitializer>();
        builder.Services.AddKeyedScoped<IRepository<Customer>, WaterRepository<Customer>>("water:customers");
        builder.Services.AddKeyedScoped<IReadRepository<Customer>, WaterRepository<Customer>>("water:customers");
        builder.Services.AddKeyedScoped<IRepository<Meter>, WaterRepository<Meter>>("water:meters");
        builder.Services.AddKeyedScoped<IReadRepository<Meter>, WaterRepository<Meter>>("water:meters");
        builder.Services.AddKeyedScoped<IRepository<MeterReading>, WaterRepository<MeterReading>>("water:meter-readings");
        builder.Services.AddKeyedScoped<IReadRepository<MeterReading>, WaterRepository<MeterReading>>("water:meter-readings");
        builder.Services.AddKeyedScoped<IRepository<Tariff>, WaterRepository<Tariff>>("water:tariffs");
        builder.Services.AddKeyedScoped<IReadRepository<Tariff>, WaterRepository<Tariff>>("water:tariffs");
        builder.Services.AddKeyedScoped<IRepository<Bill>, WaterRepository<Bill>>("water:bills");
        builder.Services.AddKeyedScoped<IReadRepository<Bill>, WaterRepository<Bill>>("water:bills");
        builder.Services.AddKeyedScoped<IRepository<Payment>, WaterRepository<Payment>>("water:payments");
        builder.Services.AddKeyedScoped<IReadRepository<Payment>, WaterRepository<Payment>>("water:payments");
        builder.Services.AddKeyedScoped<IRepository<MeterTroubleTicket>, WaterRepository<MeterTroubleTicket>>("water:trouble-tickets");
        builder.Services.AddKeyedScoped<IReadRepository<MeterTroubleTicket>, WaterRepository<MeterTroubleTicket>>("water:trouble-tickets");
        return builder;
    }

    public static WebApplication UseWaterModule(this WebApplication app)
    {
        return app;
    }
}
