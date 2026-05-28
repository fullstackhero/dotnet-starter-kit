using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Billing
{
    /// <inheritdoc />
    public partial class BillingPlanIntervalAndInvoicePurpose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_invoices_tenant_period",
                schema: "billing",
                table: "Invoices");

            migrationBuilder.AddColumn<decimal>(
                name: "AnnualPrice",
                schema: "billing",
                table: "Plans",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Interval",
                schema: "billing",
                table: "Plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PeriodEndUtc",
                schema: "billing",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PeriodStartUtc",
                schema: "billing",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Purpose",
                schema: "billing",
                table: "Invoices",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "ux_invoices_tenant_period_purpose",
                schema: "billing",
                table: "Invoices",
                columns: new[] { "TenantId", "PeriodYear", "PeriodMonth", "Purpose" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_invoices_tenant_period_purpose",
                schema: "billing",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "AnnualPrice",
                schema: "billing",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "Interval",
                schema: "billing",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "PeriodEndUtc",
                schema: "billing",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PeriodStartUtc",
                schema: "billing",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Purpose",
                schema: "billing",
                table: "Invoices");

            migrationBuilder.CreateIndex(
                name: "ux_invoices_tenant_period",
                schema: "billing",
                table: "Invoices",
                columns: new[] { "TenantId", "PeriodYear", "PeriodMonth" },
                unique: true);
        }
    }
}
