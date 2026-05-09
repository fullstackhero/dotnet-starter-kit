using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Infrastructure.Persistence;

internal sealed class WaterDbInitializer(
    ILogger<WaterDbInitializer> logger,
    WaterDbContext context) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[{Tenant}] applied database migrations for water module", context.TenantInfo!.Identifier);
        }
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("[{Tenant}] seeding water module data", context.TenantInfo!.Identifier);

        if (!await context.Customers.AnyAsync(cancellationToken))
        {
            var now = DateTimeOffset.UtcNow;

            // Customers
            var juan = Customer.Create("WTR-2024-0001", "Juan Dela Cruz", "123 Rizal St., Brgy. San Juan, Pinan, Zamboanga del Norte", "09171234567", "juan.delacruz@email.com", ConnectionType.Residential);
            var maria = Customer.Create("WTR-2024-0002", "Maria C. Santos", "456 Mabini St., Brgy. Sta. Cruz, Pinan, Zamboanga del Norte", "09179876543", "maria.santos@email.com", ConnectionType.Residential);
            var jose = Customer.Create("WTR-2024-0003", "Jose R. Rizal Jr.", "789 Bonifacio Ave., Brgy. San Roque, Pinan, Zamboanga del Norte", "09177123456", "jose.rizal@email.com", ConnectionType.Commercial);
            var ana = Customer.Create("WTR-2024-0004", "Ana Marie Gonzales", "321 Luna St., Brgy. San Francisco, Pinan, Zamboanga del Norte", "09177543210", "ana.gonzales@email.com", ConnectionType.Residential);
            var pedro = Customer.Create("WTR-2024-0005", "Pedro B. Reyes", "654 Aguinaldo St., Brgy. San Isidro, Pinan, Zamboanga del Norte", "09172345678", "pedro.reyes@email.com", ConnectionType.Industrial);

            context.Customers.AddRange(juan, maria, jose, ana, pedro);
            await context.SaveChangesAsync(cancellationToken);

            // Meters
            var meter1 = Meter.Create("MTR-1001", "Mitsubishi WP-305", now.AddYears(-3), juan.Id);
            var meter2 = Meter.Create("MTR-1002", "Mitsubishi WP-305", now.AddYears(-2), maria.Id);
            var meter3 = Meter.Create("MTR-1003", "Sensus iPERL", now.AddYears(-1), jose.Id);
            var meter4 = Meter.Create("MTR-1004", "Sensus iPERL", now.AddMonths(-6), jose.Id);
            var meter5 = Meter.Create("MTR-1005", "Mitsubishi WP-205", now.AddYears(-4), ana.Id);
            var meter6 = Meter.Create("MTR-1006", "Zenner DN15", now.AddYears(-5), pedro.Id);
            var meter7 = Meter.Create("MTR-1007", "Zenner DN20", now.AddYears(-2), pedro.Id);
            var meter8 = Meter.Create("MTR-1008", "Mitsubishi WP-305", now.AddYears(-1), juan.Id);

            context.Meters.AddRange(meter1, meter2, meter3, meter4, meter5, meter6, meter7, meter8);
            await context.SaveChangesAsync(cancellationToken);

            // Meter Readings
            var reading1 = MeterReading.Create(meter1.Id, now.AddMonths(-2), 145.2m, 120.0m, ReadingSource.Manual, "Normal reading");
            var reading2 = MeterReading.Create(meter1.Id, now.AddMonths(-1), 175.8m, 145.2m, ReadingSource.Manual, "Normal reading");
            var reading3 = MeterReading.Create(meter2.Id, now.AddMonths(-2), 98.5m, 80.0m, ReadingSource.Manual, "Normal reading");
            var reading4 = MeterReading.Create(meter2.Id, now.AddMonths(-1), 125.3m, 98.5m, ReadingSource.Manual, "Normal reading");
            var reading5 = MeterReading.Create(meter3.Id, now.AddMonths(-2), 450.0m, 380.0m, ReadingSource.Automated, null);
            var reading6 = MeterReading.Create(meter3.Id, now.AddMonths(-1), 520.7m, 450.0m, ReadingSource.Automated, null);
            var reading7 = MeterReading.Create(meter4.Id, now.AddMonths(-2), 180.3m, 150.0m, ReadingSource.Manual, "New installation");
            var reading8 = MeterReading.Create(meter4.Id, now.AddMonths(-1), 210.9m, 180.3m, ReadingSource.Manual, "Normal reading");
            var reading9 = MeterReading.Create(meter5.Id, now.AddMonths(-2), 67.8m, 50.0m, ReadingSource.Manual, "Normal reading");
            var reading10 = MeterReading.Create(meter5.Id, now.AddMonths(-1), 89.4m, 67.8m, ReadingSource.Manual, "Normal reading");
            var reading11 = MeterReading.Create(meter6.Id, now.AddMonths(-2), 890.5m, 750.0m, ReadingSource.Automated, null);
            var reading12 = MeterReading.Create(meter6.Id, now.AddMonths(-1), 1020.3m, 890.5m, ReadingSource.Automated, null);
            var reading13 = MeterReading.Create(meter7.Id, now.AddMonths(-2), 340.1m, 280.0m, ReadingSource.Automated, null);
            var reading14 = MeterReading.Create(meter7.Id, now.AddMonths(-1), 395.6m, 340.1m, ReadingSource.Automated, null);
            var reading15 = MeterReading.Create(meter8.Id, now.AddMonths(-2), 55.2m, 40.0m, ReadingSource.Manual, "Rental property");
            var reading16 = MeterReading.Create(meter8.Id, now.AddMonths(-1), 72.8m, 55.2m, ReadingSource.Manual, "Rental property");

            context.MeterReadings.AddRange(reading1, reading2, reading3, reading4, reading5, reading6, reading7, reading8, reading9, reading10, reading11, reading12, reading13, reading14, reading15, reading16);
            await context.SaveChangesAsync(cancellationToken);

            // Tariffs
            var residential = Tariff.Create("Residential Rate", "Standard residential water rate", now.AddMonths(-12), null, 15.00m, 50.00m);
            var commercial = Tariff.Create("Commercial Rate", "Commercial establishment water rate", now.AddMonths(-12), null, 25.00m, 200.00m);
            var industrial = Tariff.Create("Industrial Rate", "Industrial facility water rate", now.AddMonths(-12), null, 20.00m, 500.00m);

            context.Tariffs.AddRange(residential, commercial, industrial);
            await context.SaveChangesAsync(cancellationToken);

            // Bills
            var bill1 = Bill.Create(juan.Id, residential.Id, now.Month, now.Year, 30.6m, 50.00m, 459.00m, 509.00m, now.AddDays(15));
            bill1.MarkAsPublished();

            var bill2 = Bill.Create(maria.Id, residential.Id, now.Month, now.Year, 26.8m, 50.00m, 402.00m, 452.00m, now.AddDays(15));
            bill2.MarkAsPublished();

            var bill3 = Bill.Create(jose.Id, commercial.Id, now.Month, now.Year, 101.3m, 200.00m, 2532.50m, 2732.50m, now.AddDays(15));
            bill3.MarkAsPublished();

            var bill4 = Bill.Create(ana.Id, residential.Id, now.Month, now.Year, 21.6m, 50.00m, 324.00m, 374.00m, now.AddDays(15));
            bill4.MarkAsPublished();

            var bill5 = Bill.Create(pedro.Id, industrial.Id, now.Month, now.Year, 185.3m, 500.00m, 3706.00m, 4206.00m, now.AddDays(15));
            bill5.MarkAsPublished();

            context.Bills.AddRange(bill1, bill2, bill3, bill4, bill5);
            await context.SaveChangesAsync(cancellationToken);

            // Payments
            var payment1 = Payment.Create(bill1.Id, 509.00m, now.AddDays(-5), PaymentMethod.Online, "REF-ONL-001");
            bill1.MarkAsPaid(now.AddDays(-5));

            var payment2 = Payment.Create(bill2.Id, 452.00m, now.AddDays(-3), PaymentMethod.Cash, "REF-CSH-001");
            bill2.MarkAsPaid(now.AddDays(-3));

            var payment3 = Payment.Create(bill4.Id, 374.00m, now.AddDays(-1), PaymentMethod.BankTransfer, "REF-BNK-001");
            bill4.MarkAsPaid(now.AddDays(-1));

            context.Payments.AddRange(payment1, payment2, payment3);
            await context.SaveChangesAsync(cancellationToken);

            // Meter Trouble Tickets
            var ticket1 = MeterTroubleTicket.Create(meter5.Id, now.AddDays(-20), "Meter display not showing reading. Need technician to inspect and replace if necessary.");
            ticket1.Resolve("Replaced LCD display panel. Meter now functioning normally.");

            var ticket2 = MeterTroubleTicket.Create(meter2.Id, now.AddDays(-10), "Suspected water leak near meter connection. Customer reported unusually high consumption.");

            context.MeterTroubleTickets.AddRange(ticket1, ticket2);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("[{Tenant}] seeded water module data with Filipino sample records", context.TenantInfo!.Identifier);
        }
    }
}
